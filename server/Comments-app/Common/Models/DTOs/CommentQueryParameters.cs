namespace CommentApp.Common.Models.DTOs
{
    public class CommentQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 25;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value > 100)
                    _pageSize = 100;
                else if (value <= 0)
                    _pageSize = 10;
                else
                    _pageSize = value;
            }
        }
        public string SortBy { get; set; } = "UserName";
        public string SortDirection { get; set; } = "asc";
    }
}
