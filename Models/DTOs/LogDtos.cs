namespace HotelManagementApi.Models.DTOs
{
    public class LogFileDto
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Size { get; set; }
    }

    public class LogLineDto
    {
        public int Id { get; set; }
        public string Time { get; set; }
        public string Level { get; set; }
        public string Text { get; set; }
    }
}