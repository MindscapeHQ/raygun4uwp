using Raygun4UWP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Raygun4UWP
{
    public interface IRaygunBreadcrumbStorage
    {
        void Store(RaygunBreadcrumb breadcrumb);

        void Clear();

        int Size();

        IList<RaygunBreadcrumb> ToList();
    }

    /// <summary>
    /// Stores and retrieves breadcrumbs.
    /// Based on the implementation in Raygun4Net. 
    /// i.e. Mindscape.Raygun4Net.NetCore.Common/Breadcrumbs
    /// </summary>
    public class RaygunBreadcrumbs
    {
        private readonly IRaygunBreadcrumbStorage _storage = new InMemoryBreadcrumbStorage();

        public void Record(RaygunBreadcrumb crumb)
        {
            _storage.Store(crumb);
        }

        public void Clear()
        {
            _storage.Clear();
        }

        public int Size()
        {
            return _storage.Size();
        }

        public IList<RaygunBreadcrumb> ToList()
        {
            return _storage.ToList();
        }
    }

    public class InMemoryBreadcrumbStorage : IRaygunBreadcrumbStorage
    {
        private readonly List<RaygunBreadcrumb> _breadcrumbs;

        private const int MaxSize = 25;

        public InMemoryBreadcrumbStorage(List<RaygunBreadcrumb> breadcrumbs = null)
        {
            _breadcrumbs = breadcrumbs ?? new List<RaygunBreadcrumb>();
        }

        public void Store(RaygunBreadcrumb breadcrumb)
        {
            if (_breadcrumbs == null)
            {
                return;
            }

            if (_breadcrumbs.Count == MaxSize)
            {
                _breadcrumbs.RemoveAt(0);
            }

            _breadcrumbs.Add(breadcrumb);
        }

        public void Clear()
        {
            _breadcrumbs?.Clear();
        }

        public int Size()
        {
            return _breadcrumbs?.Count ?? 0;
        }

        public IList<RaygunBreadcrumb> ToList()
        {
            // Copy the list to avoid external modification  
            // and return a new list  
            if (_breadcrumbs == null)
            {
                return new List<RaygunBreadcrumb>();
            }
            return new List<RaygunBreadcrumb>(_breadcrumbs);
        }
    }
}