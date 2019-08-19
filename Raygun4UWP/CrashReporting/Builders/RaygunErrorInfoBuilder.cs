﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Raygun4UWP
{
  public class RaygunErrorInfoBuilder
  {
    private const int SIGNATURE_OFFSET_OFFSET = 60; // 0x3c
    private const int SIGNATURE_SIZE = 4;
    private const int COFF_FILE_HEADER_SIZE = 20;

    private const int DEBUG_DATA_DIRECTORY_OFFSET_32 = 144;
    private const int DEBUG_DATA_DIRECTORY_OFFSET_64 = 160;
    private const int DEBUG_DIRECTORY_SIZE = 28;

    public static RaygunErrorInfo Build(Exception exception)
    {
      RaygunErrorInfo message = new RaygunErrorInfo();

      var exceptionType = exception.GetType();
      
      message.Message = exception.Message;
      message.ClassName = FormatTypeName(exceptionType, true);

      List<RaygunImageInfo> images = new List<RaygunImageInfo>();

      try
      {
        message.StackTrace = BuildStackTrace(exception);
      }
      catch (Exception e)
      {
        Debug.WriteLine(string.Format($"Failed to get managed stack trace information: {e.Message}"));
      }

      try
      {
        message.NativeStackTrace = BuildNativeStackTrace(images, exception);
      }
      catch (Exception e)
      {
        Debug.WriteLine(string.Format($"Failed to get native stack trace information: {e.Message}"));
      }

      message.Data = exception.Data;

      AggregateException ae = exception as AggregateException;
      if (ae?.InnerExceptions != null)
      {
        message.InnerErrors = new RaygunErrorInfo[ae.InnerExceptions.Count];
        int index = 0;
        foreach (Exception e in ae.InnerExceptions)
        {
          message.InnerErrors[index] = Build(e);
          index++;
        }
      }
      else if (exception.InnerException != null)
      {
        message.InnerError = Build(exception.InnerException);
      }

      if (images.Count > 0)
      {
        message.Images = images.ToArray();
      }

      return message;
    }

    private static string FormatTypeName(Type type, bool fullName)
    {
      string name = fullName ? type.FullName : type.Name;
      Type[] genericArguments = type.GenericTypeArguments;
      if (genericArguments.Length == 0)
      {
        return name;
      }

      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(name.Substring(0, name.IndexOf("`")));
      stringBuilder.Append("<");
      foreach (Type t in genericArguments)
      {
        stringBuilder.Append(FormatTypeName(t, false)).Append(",");
      }
      stringBuilder.Remove(stringBuilder.Length - 1, 1);
      stringBuilder.Append(">");

      return stringBuilder.ToString();
    }

    private static RaygunStackTraceFrame[] BuildStackTrace(Exception exception)
    {
      var lines = new List<RaygunStackTraceFrame>();

      //TODO: there are parsing improvements we can make for when code optimization is disabled
      if (exception.StackTrace != null)
      {
        char[] separators = {'\r', '\n'};
        var frames = exception.StackTrace.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in frames)
        {
          // Trim the stack trace line
          string stackTraceLine = line.Trim();
          if (stackTraceLine.StartsWith("at "))
          {
            stackTraceLine = stackTraceLine.Substring(3);
          }

          RaygunStackTraceFrame stackTraceLineMessage = new RaygunStackTraceFrame
          {
            Raw = stackTraceLine
          };

          // Extract the method name and class name if possible:
          int index = stackTraceLine.IndexOf("(");
          if (index > 0)
          {
            index = stackTraceLine.LastIndexOf(".", index);
            if (index > 0)
            {
              stackTraceLineMessage.ClassName = stackTraceLine.Substring(0, index);
              stackTraceLineMessage.MethodName = stackTraceLine.Substring(index + 1);
            }
          }

          lines.Add(stackTraceLineMessage);
        }
      }

      return lines.Count == 0 ? null : lines.ToArray();
    }

    private static RaygunNativeStackTraceFrame[] BuildNativeStackTrace(List<RaygunImageInfo> images, Exception exception)
    {
      var lines = new List<RaygunNativeStackTraceFrame>();
      
      var stackTrace = new StackTrace(exception, true);
      var frames = stackTrace.GetFrames();

      foreach (StackFrame frame in frames)
      {
        if (frame.HasNativeImage())
        {
          IntPtr nativeIP = frame.GetNativeIP();
          IntPtr nativeImageBase = frame.GetNativeImageBase();

          long nativeImageBaseLong = nativeImageBase.ToInt64();
          
          if (images.All(i => i.BaseAddress != nativeImageBaseLong))
          {
            // PE Format:
            // -----------
            // https://docs.microsoft.com/en-us/windows/win32/debug/pe-format
            // -----------
            // MS-DOS Stub
            // Signature (4 bytes, offset to this can be found at 0x3c)
            // COFF File Header (20 bytes)
            // Optional header (variable size)
            //   Standard fields
            //   Windows-specific fields
            //   Data directories (position 96/112)
            //     ...
            //     Debug (8 bytes, position 144/160)
            //     ...

            // TODO: use SizeOfOptionalHeader and NumberOfRvaAndSizes before tapping into data directories

            // All offset values are relative to the nativeImageBase
            int signatureOffset = CopyInt32(nativeImageBase + SIGNATURE_OFFSET_OFFSET);

            int optionalHeaderOffset = signatureOffset + SIGNATURE_SIZE + COFF_FILE_HEADER_SIZE;

            short magic = CopyInt16(nativeImageBase + optionalHeaderOffset);
            
            int debugDataDirectoryOffset = optionalHeaderOffset + (magic == (short) PEMagic.PE32 ? DEBUG_DATA_DIRECTORY_OFFSET_32 : DEBUG_DATA_DIRECTORY_OFFSET_64);

            // TODO: this address can be 0 if there is no debug information:
            int debugVirtualAddress = CopyInt32(nativeImageBase + debugDataDirectoryOffset);
            
            int debugSize = CopyInt32(nativeImageBase + debugDataDirectoryOffset + 4);

            int debugDirectoryCount = debugSize / DEBUG_DIRECTORY_SIZE;

            RaygunImageInfo image = new RaygunImageInfo
            {
              BaseAddress = nativeImageBaseLong,
              DebugInfo = new RaygunImageDebugInfo[debugDirectoryCount]
            };

            for (int i = 0; i < debugDirectoryCount; i++)
            {
              int debugDirectoryAddress = debugVirtualAddress + (i * DEBUG_DIRECTORY_SIZE);
              
              // TODO: check that this is 2
              int type = CopyInt32(nativeImageBase + debugDirectoryAddress + 12);

              int sizeOfData = CopyInt32(nativeImageBase + debugDirectoryAddress + 16);

              int addressOfRawData = CopyInt32(nativeImageBase + debugDirectoryAddress + 20);
              
              // Debug information:
              // Reference: http://www.godevtool.com/Other/pdb.htm

              // TODO: check that this is "RSDS" before looking into subsequent values
              int debugSignature = CopyInt32(nativeImageBase + addressOfRawData);

              byte[] debugGuidArray = new byte[16];
              Marshal.Copy(nativeImageBase + addressOfRawData + 4, debugGuidArray, 0, 16);
              Guid debugGuid = new Guid(debugGuidArray);
              
              byte[] fileNameArray = new byte[sizeOfData - 24];
              Marshal.Copy(nativeImageBase + addressOfRawData + 24, fileNameArray, 0, sizeOfData - 24);

              string pdbFileName = Encoding.UTF8.GetString(fileNameArray, 0, fileNameArray.Length);

              image.DebugInfo[i] = new RaygunImageDebugInfo
              {
                PdbFileName = pdbFileName,
                Guid = debugGuid.ToString()
              };
            }
            
            images.Add(image);
          }
          
          var line = new RaygunNativeStackTraceFrame
          {
            IP = nativeIP.ToInt64(),
            ImageBase = nativeImageBaseLong
          };
          
          lines.Add(line);
        }
      }

      return lines.Count == 0 ? null : lines.ToArray();
    }

    private static short CopyInt16(IntPtr address)
    {
      byte[] byteArray = new byte[2];
      Marshal.Copy(address, byteArray, 0, 2);
      return BitConverter.ToInt16(byteArray, 0);
    }

    private static int CopyInt32(IntPtr address)
    {
      byte[] byteArray = new byte[4];
      Marshal.Copy(address, byteArray, 0, 4);
      return BitConverter.ToInt32(byteArray, 0);
    }
  }
}
