using System;
using System.Collections.Generic;
using SnomiAssignmentReal.Models;

namespace SnomiAssignmentReal.Models.ViewModels
{
    public class ManageOrderHistoryVm
    {
        public List<CustomerOrder> Orders { get; set; } = new();

        // Filters
        public string? Q { get; set; }                      // search term
        public string Status { get; set; } = "all";         // all | served | completed | cancelled
        public string Paid { get; set; } = "all";           // all | yes | no
        public string Method { get; set; } = "all";         // all | Cash | Card | E-Wallet | QR
        public string CustomerType { get; set; } = "all";   // all | guest | registered
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public decimal? MinTotal { get; set; }
        public decimal? MaxTotal { get; set; }
        public string Sort { get; set; } = "recent_desc";   // recent_desc|recent_asc|amount_desc|amount_asc|items_desc|items_asc

        // Paging
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int Total { get; set; }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((decimal)Math.Max(0, Total) / Math.Max(1, PageSize)));
    }
}
