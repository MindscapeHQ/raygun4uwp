using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Raygun4UWP
{
  public static class RaygunErrorInfoBuilder
  {
    private const string CLASS_NAME_PREFIX = "at ";
    private const string METHOD_NAME_PREFIX = ".";
    private const string FILE_NAME_PREFIX = " in ";
    private const string LINE_NUMBER_PREFIX = ":line ";

    private const int SIGNATURE_OFFSET_OFFSET = 60; // 0x3c
    private const int SIGNATURE_SIZE = 4;
    private const int COFF_FILE_HEADER_SIZE = 20;

    private const int DEBUG_DATA_DIRECTORY_OFFSET_32 = 144;
    private const int DEBUG_DATA_DIRECTORY_OFFSET_64 = 160;
    private const int DEBUG_DIRECTORY_SIZE = 28;

    private const int RSDS_SIGNATURE = 0x53445352; // "RSDS"

    public static RaygunErrorInfo Build(Exception exception)
    {
      Dictionary<IntPtr, RaygunImageInfo> images = new Dictionary<IntPtr, RaygunImageInfo>();

      RaygunErrorInfo errorInfo = BuildErrorInfo(exception, images);

      if (images.Count > 0)
      {
        errorInfo.Images = images.Values.Where(i => i != null).ToArray();
      }

      return errorInfo;
    }

    private static RaygunErrorInfo BuildErrorInfo(Exception exception, Dictionary<IntPtr, RaygunImageInfo> images)
    {
      RaygunErrorInfo errorInfo = new RaygunErrorInfo();

      var exceptionType = exception.GetType();

      errorInfo.Message = exception.Message;
      errorInfo.ClassName = FormatTypeName(exceptionType, true);

      try
      {
        errorInfo.StackTrace = BuildNativeStackTrace(images, exception);
      }
      catch (Exception e)
      {
        Debug.WriteLine($"Failed to get native stack trace information: {e.Message}");
      }

      if (errorInfo.StackTrace == null)
      {
        try
        {
          errorInfo.StackTrace = BuildStackTrace(exception);
        }
        catch (Exception e)
        {
          Debug.WriteLine($"Failed to get managed stack trace information: {e.Message}");
        }
      }

      errorInfo.Data = exception.Data;

      AggregateException ae = exception as AggregateException;
      if (ae?.InnerExceptions != null)
      {
        errorInfo.InnerErrors = new RaygunErrorInfo[ae.InnerExceptions.Count];
        int index = 0;
        foreach (Exception e in ae.InnerExceptions)
        {
          errorInfo.InnerErrors[index] = BuildErrorInfo(e, images);
          index++;
        }
      }
      else if (exception.InnerException != null)
      {
        errorInfo.InnerError = BuildErrorInfo(exception.InnerException, images);
      }

      return errorInfo;
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

      if (exception.StackTrace != null)
      {
        char[] separators = {'\r', '\n'};
        var frames = exception.StackTrace.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in frames)
        {
          // Trim the stack trace line
          string stackTraceLine = line.Trim();
          if (stackTraceLine.StartsWith(CLASS_NAME_PREFIX))
          {
            stackTraceLine = stackTraceLine.Substring(CLASS_NAME_PREFIX.Length);
          }

          RaygunStackTraceFrame stackTraceFrame = new RaygunStackTraceFrame
          {
            Raw = stackTraceLine
          };

          ParseRawStackTraceLine(stackTraceFrame);

          lines.Add(stackTraceFrame);
        }
      }

      return lines.Count == 0 ? null : lines.ToArray();
    }

    private static void ParseRawStackTraceLine(RaygunStackTraceFrame stackTraceFrame)
    {
      if (!string.IsNullOrWhiteSpace(stackTraceFrame.Raw))
      {
        string stackTraceLine = stackTraceFrame.Raw;

        // Find the separation between class name and method name:
        int methodNameIndex = stackTraceLine.IndexOf("(");
        if (methodNameIndex > 0)
        {
          if (stackTraceLine[methodNameIndex - 1] == '>')
          {
            // The method has generic parameters, so we need to do a little more work to find the separation between class and method names

            methodNameIndex -= 2; // start before the angle bracket we just found
            int angleBracketPairing = 1;
            while (methodNameIndex > 0 && angleBracketPairing != 0)
            {
              char c = stackTraceLine[methodNameIndex];
              if (c == '>')
              {
                angleBracketPairing++;
              }
              else if (c == '<')
              {
                angleBracketPairing--;
              }

              methodNameIndex--;
            }
          }

          methodNameIndex = stackTraceLine.LastIndexOf(METHOD_NAME_PREFIX, methodNameIndex);
          if (methodNameIndex > 0)
          {
            // After finding the separation between class name and method name, the class name is the first part of the string:
            stackTraceFrame.ClassName = stackTraceLine.Substring(0, methodNameIndex);

            // Find the separation between method name and file name:
            methodNameIndex += METHOD_NAME_PREFIX.Length;
            int fileNameIndex = stackTraceLine.IndexOf(FILE_NAME_PREFIX, methodNameIndex);
            if (fileNameIndex > 0)
            {
              stackTraceFrame.MethodName = stackTraceLine.Substring(methodNameIndex, fileNameIndex - methodNameIndex);

              // Find the separation between file name and line number:
              fileNameIndex += FILE_NAME_PREFIX.Length;
              int lineNumberIndex = stackTraceLine.IndexOf(LINE_NUMBER_PREFIX, fileNameIndex);
              if (lineNumberIndex > 0)
              {
                stackTraceFrame.FileName = stackTraceLine.Substring(fileNameIndex, lineNumberIndex - fileNameIndex);

                // Parse the line number:
                lineNumberIndex += LINE_NUMBER_PREFIX.Length;
                string lineNumberStr = stackTraceLine.Substring(lineNumberIndex);

                if (int.TryParse(lineNumberStr, out var lineNumber))
                {
                  stackTraceFrame.LineNumber = lineNumber;
                }
              }
              else
              {
                // In this case, the frame does not have a line number:
                stackTraceFrame.FileName = stackTraceLine.Substring(fileNameIndex);
              }
            }
            else
            {
              // In this case, the frame only has method and class names:
              stackTraceFrame.MethodName = stackTraceLine.Substring(methodNameIndex);
            }
          }
        }
      }
    }

    private static RaygunStackTraceFrame[] BuildNativeStackTrace(Dictionary<IntPtr, RaygunImageInfo> images, Exception exception)
    {
      var lines = new List<RaygunStackTraceFrame>();

      var stackTrace = new StackTrace(exception, true);
      var frames = stackTrace.GetFrames();

      foreach (StackFrame frame in frames)
      {
        if (frame.HasNativeImage())
        {
          IntPtr nativeIP = frame.GetNativeIP();
          IntPtr nativeImageBase = frame.GetNativeImageBase();

          if (!images.ContainsKey(nativeImageBase))
          {
            RaygunImageInfo imageInfo = ReadImage(nativeImageBase);
            images[nativeImageBase] = imageInfo;
          }

          var line = new RaygunStackTraceFrame
          {
            IP = nativeIP.ToInt64(),
            ImageBase = nativeImageBase.ToInt64(),
            Raw = frame.ToString()
          };

          ParseRawStackTraceLine(line);

          lines.Add(line);
        }
      }

      return lines.Count == 0 ? null : lines.ToArray();
    }

    private static RaygunImageInfo ReadImage(IntPtr nativeImageBase)
    {
      // PE Format:
      // -----------
      // Reference: https://docs.microsoft.com/en-us/windows/win32/debug/pe-format
      // -----------
      // MS-DOS Stub
      // Signature (4 bytes, offset to this can be found at 0x3c which is within the MS-DOS Stub)
      // COFF File Header (20 bytes)
      //   SizeOfOptionalHeader (2 bytes, position 16)
      // Optional header (variable size)
      //   Standard fields
      //     Magic (2 bytes, position 0)
      //   Windows-specific fields
      //   Data directories
      //     ...
      //     Debug (8 bytes, position 144/160 relative to optional header)
      //     ...

      int signatureOffset = CopyInt32(nativeImageBase + SIGNATURE_OFFSET_OFFSET);

      short sizeOfOptionalHeader = CopyInt16(nativeImageBase + signatureOffset + SIGNATURE_SIZE + 16);

      int optionalHeaderOffset = signatureOffset + SIGNATURE_SIZE + COFF_FILE_HEADER_SIZE;

      short magic = CopyInt16(nativeImageBase + optionalHeaderOffset);

      int debugDataDirectoryOffset = optionalHeaderOffset + (magic == (short) PEMagic.PE32 ? DEBUG_DATA_DIRECTORY_OFFSET_32 : DEBUG_DATA_DIRECTORY_OFFSET_64);

      if (debugDataDirectoryOffset < optionalHeaderOffset + sizeOfOptionalHeader)
      {
        List<RaygunImageDebugInfo> debugInfo = ReadDebugDataDirectory(nativeImageBase, debugDataDirectoryOffset);

        if (debugInfo != null && debugInfo.Count > 0)
        {
          return new RaygunImageInfo
          {
            BaseAddress = nativeImageBase.ToInt64(),
            DebugInfo = debugInfo.ToArray()
          };
        }
      }

      return null;
    }

    private static List<RaygunImageDebugInfo> ReadDebugDataDirectory(IntPtr nativeImageBase, int rva)
    {
      // Optional header data directories:
      // ----------------------------------
      // Reference: https://docs.microsoft.com/en-us/windows/win32/debug/pe-format#optional-header-data-directories-image-only
      // ----------------------------------
      // +0    dword    the relative virtual address of the directory
      // +4    dword    the size in bytes of the directory

      int debugVirtualAddress = CopyInt32(nativeImageBase + rva);

      if (debugVirtualAddress != 0)
      {
        int debugSize = CopyInt32(nativeImageBase + rva + 4);

        return ReadDebugDirectories(nativeImageBase, debugVirtualAddress, debugSize);
      }

      return null;
    }

    private static List<RaygunImageDebugInfo> ReadDebugDirectories(IntPtr nativeImageBase, int rva, int size)
    {
      // Debug directories:
      // -------------------
      // Reference: https://docs.microsoft.com/en-us/windows/win32/debug/pe-format#the-debug-section
      // -------------------
      // +0     dword    characteristics - reserved, must be zero
      // +4     dword    the time and date that the debug data was created
      // +8     word     the major version number of the debug data format
      // +10    word     the minor version number of the debug data format
      // +12    dword    the format of debugging information
      // +16    dword    the size of the debug data(not including the debug directory itself)
      // +20    dword    the address of the debug data when loaded, relative to the image base
      // +24    dword    the file pointer to the debug data

      List<RaygunImageDebugInfo> debugInfo = new List<RaygunImageDebugInfo>();

      int debugDirectoryCount = size / DEBUG_DIRECTORY_SIZE;

      for (int i = 0; i < debugDirectoryCount; i++)
      {
        int debugDirectoryAddress = rva + (i * DEBUG_DIRECTORY_SIZE);

        int type = CopyInt32(nativeImageBase + debugDirectoryAddress + 12);

        if (type == (int) DebugDirectoryEntryType.CodeView)
        {
          int sizeOfData = CopyInt32(nativeImageBase + debugDirectoryAddress + 16);

          int addressOfRawData = CopyInt32(nativeImageBase + debugDirectoryAddress + 20);

          RaygunImageDebugInfo raygunImageDebugInfo = ReadDebugInformation(nativeImageBase, addressOfRawData, sizeOfData);
          if (raygunImageDebugInfo != null)
          {
            debugInfo.Add(raygunImageDebugInfo);
          }
        }
      }

      return debugInfo;
    }

    private static RaygunImageDebugInfo ReadDebugInformation(IntPtr nativeImageBase, int rva, int size)
    {
      // Debug information:
      // -------------------
      // +0     dword     "RSDS" signature
      // +4     GUID      16 - byte Globally Unique Identifier
      // +20    dword     a value which is incremented each time the executable and its associated pdb file is remade by the linker 
      // +24    string    zero terminated UTF8 PDB path and file name

      int debugSignature = CopyInt32(nativeImageBase + rva);

      if (debugSignature == RSDS_SIGNATURE)
      {
        byte[] debugGuidArray = new byte[16];
        Marshal.Copy(nativeImageBase + rva + 4, debugGuidArray, 0, 16);
        Guid debugGuid = new Guid(debugGuidArray);

        // We subtract an extra 1 here to discard the zero terminator
        int fileNameSize = size - 24 - 1;
        if (fileNameSize > 0)
        {
          byte[] fileNameArray = new byte[fileNameSize];
          Marshal.Copy(nativeImageBase + rva + 24, fileNameArray, 0, fileNameSize);

          string pdbFileName = Encoding.UTF8.GetString(fileNameArray, 0, fileNameArray.Length);

          return new RaygunImageDebugInfo
          {
            PdbFileName = pdbFileName,
            Guid = debugGuid.ToString()
          };
        }
      }

      return null;
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
