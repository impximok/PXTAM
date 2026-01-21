using System;
using System.Collections.Generic;
using SnomiAssignmentReal.Models;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class ManageOrdersVm
    {
        public List<CustomerOrder> Orders { get; set; } = new();
        public string? Q { get; set; }
        public string? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int Total { get; set; }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / Math.Max(1, PageSize)));
    }
}
