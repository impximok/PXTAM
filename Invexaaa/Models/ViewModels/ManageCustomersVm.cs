using SnomiAssignmentReal.Models;
using System;
using System.Collections.Generic;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class ManageCustomersVm
    {
        public List<Customer> Customers { get; set; } = new();

        // filters
        public string? Search { get; set; }
        public string UserType { get; set; } = "all";   // all|registered|guests
        public string OrdersFilter { get; set; } = "all";
        public string SortBy { get; set; } = "recent_desc";
        public int? MinPoints { get; set; }
        public int? MaxPoints { get; set; }

        // paging
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalItems / Math.Max(1, PageSize)));
    }
}
