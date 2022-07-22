using Google.Protobuf.WellKnownTypes;
using Google.Protobuf.Collections;
using Grpc.Core;
using ON.Fragments.Content;
using ON.Fragments.Generic;
using ON.Content.Rumble.Service.Models;
using Microsoft.Extensions.Options;

namespace ON.Content.Rumble.Service
{
    public class VideoService : VideoInterface.VideoInterfaceBase
    {
        private readonly ILogger<ServiceOpsService> logger;
        private readonly IFileSystemRumbleProvider rumbleProvider;
        private readonly IOptions<AppSettings> settings;

        public VideoService(ILogger<ServiceOpsService> logger, IOptions<AppSettings> settings, IFileSystemRumbleProvider rumbleProvider)
        {
            this.logger = logger;
            this.rumbleProvider = rumbleProvider;
            this.settings = settings;
        }

        public async Task<DataResponse> GetData(GetDataRequest request)
        {
            await Task.Delay(500);
            try
            {
             if (request.Options == null || request.Options.Provider == null)
                {
                    foreach (var item in settings.Value.DataStore)
                    {
                        switch (item)
                        {
                            default:
                                logger.LogWarning($":::::ITEM:::: {item}");
                                logger.LogWarning($":::::Settings::::{settings.Value.DataStore[item]}");
                                return new DataResponse();
                        }
                    }
                }

                return new DataResponse();

            } catch (Exception ex)
            {
                return new DataResponse
                {
                    Success = false,
                    Msg = "An Error Occured",
                    Error = ex.Message
                };
            }
        }


    }
}
