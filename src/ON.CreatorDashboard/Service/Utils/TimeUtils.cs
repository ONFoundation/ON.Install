namespace ON.CreatorDashboard.Service.Utils
{
    public class TimeUtils
    {
        public static TimeSpan TimeSpanFromString(DateTime startDate, string length)
        {
            try
            {
                var endDate = DateTime.Parse(length);
                return endDate - startDate;
            } catch (FormatException format)
            {
                throw new FormatException("Invalid Format: ", format);
            }
        }

    }
}
