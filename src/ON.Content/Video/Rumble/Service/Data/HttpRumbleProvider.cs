using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ON.Content.Rumble.Service.Models;
using ON.Fragments.Content;
using RestSharp;
using System.Text.Json;

namespace ON.Content.Rumble.Service.Data
{
    public class HttpRumbleProvider
    {
        private readonly ILogger<ServiceOpsService> logger;
        private readonly IOptions<AppSettings> appSettings;
        private readonly string RumbleUri;
        public HttpRumbleProvider(ILogger<ServiceOpsService> logger, IOptions<AppSettings> settings)
        {
            this.logger = logger;
            RumbleUri = "https://rumble.com/api/v0";
            appSettings = settings;
        }

        private async Task<RestResponse> MakeHttpRequest(RestRequest request)
        {
            var client = new RestClient();
            var response = await client.GetAsync(request);
            return response;
        }

        private Result MapResponse(RestResponse response)
        {
            if (response.Content == null) { return null; }
            var responseMapping = JsonConvert.DeserializeObject<Result>(response.Content);

            if (responseMapping == null) { return null; }

            return responseMapping;
        }

        public async Task<Result> MediaItemRequest(RumbleVideoRequest request)
        {
            var uri = RumbleUri + "/Media.Item?_p=" + appSettings.Value.RumblePlatformToken;
            RestRequest req = new RestRequest(uri);
            req.AddHeader("ContentType", "application/json");
            var reqParams = new
            {
                fid = request.VideoId,
            };
            req.AddObject(reqParams);
            RestResponse resp = await MakeHttpRequest(req);
            var responseMap = MapResponse(resp);
            return responseMap;
        }

        public async Task<RestResponse> MediaSearchRequest(RumbleChannelRequest request)
        {
            var uri = RumbleUri + "/Media.Search?_p=" + appSettings.Value.RumblePlatformToken;
            var req = new RestRequest(uri);
            req.AddHeader("ContentType", "application/json");
            req.AddParameter("channel", request.ChannelId);
            var response = await MakeHttpRequest(req);
            return response;
        }

        public async Task<RestResponse> MediaSearchRequestPaginated(RumbleChannelRequest request, string pg)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var uri = RumbleUri + "/Media.Search?_p=" + appSettings.Value.RumblePlatformToken;
            var req = new RestRequest(uri);
            req.AddHeader("ContentType", "application/json");
            req.AddParameter("channel", request.ChannelId.ToString());
            req.AddParameter("criteria", "pg=" + pg);
            var response = await MakeHttpRequest(req);
            return response;
        }
    }
}
