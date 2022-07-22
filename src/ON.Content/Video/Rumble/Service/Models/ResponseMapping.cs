namespace ON.Content.Rumble.Service.Models
{
    public class ResponseMapping
    {
        public Result[] results { get; set; }
        public Paginate paginate { get; set; }
        public Criteria criteria { get; set; }
    }
}
