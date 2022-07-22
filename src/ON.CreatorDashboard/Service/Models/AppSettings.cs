namespace ON.CreatorDashboard.Service.Models
{
    public class AppSettings
    {
        public string DataStore { get; set; } = "/data";

        //public bool ContainsSecrets
        //{
        //    get
        //    {
        //        if (string.IsNullOrWhiteSpace(StripeUrl))
        //            return false;
        //        if (string.IsNullOrWhiteSpace(StripeAccount))
        //            return false;
        //        if (string.IsNullOrWhiteSpace(StripeClientID))
        //            return false;
        //        if (string.IsNullOrWhiteSpace(StripeClientSecret))
        //            return false;
        //        return true;
        //    }
        //}
    }
}


