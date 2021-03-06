using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ON.Authentication.SimpleAuth.Service.Data;
using ON.Authentication.SimpleAuth.Service.Helpers;
using ON.Crypto;
using ON.Fragments.Authentication;
using ON.Fragments.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ON.Authentication.SimpleAuth.Service.Services
{
    public class BackupService : BackupInterface.BackupInterfaceBase
    {
        private readonly IUserDataProvider dataProvider;
        private readonly ILogger<BackupService> logger;

        public BackupService(IUserDataProvider dataProvider, ILogger<BackupService> logger)
        {
            this.dataProvider = dataProvider;
            this.logger = logger;
        }

        public override async Task BackupAllData(BackupAllDataRequest request, IServerStreamWriter<BackupAllDataResponse> responseStream, ServerCallContext context)
        {
            try
            {
                var userToken = ONUserHelper.ParseUser(context.GetHttpContext());
                if (userToken == null || !userToken.Roles.Contains(ONUser.ROLE_BACKUP))
                    return;

                var encKey = EcdhHelper.DeriveKeyServer(request.ClientPublicJwk.DecodeJsonWebKey(), out string serverPubKey);
                await responseStream.WriteAsync(new BackupAllDataResponse() { ServerPublicJwk = serverPubKey });

                await foreach (var r in dataProvider.GetAll())
                {
                    var dr = new UserBackupDataRecord()
                    {
                        Data = r
                    };

                    AesHelper.Encrypt(encKey, out var iv, dr.ToByteString().ToByteArray(), out var encData);

                    await responseStream.WriteAsync(new BackupAllDataResponse()
                    {
                        EncryptedRecord = new EncryptedUserBackupDataRecord()
                        {
                            EncryptionIV = ByteString.CopyFrom(iv),
                            Data = ByteString.CopyFrom(encData)
                        }
                    });
                }
            }
            catch
            {
            }
        }

        public override async Task ExportUsers(ExportUsersRequest request, IServerStreamWriter<ExportUsersResponse> responseStream, ServerCallContext context)
        {
            try
            {
                var userToken = ONUserHelper.ParseUser(context.GetHttpContext());
                if (userToken == null || !(userToken.Roles.Contains(ONUser.ROLE_BACKUP) || userToken.Roles.Contains(ONUser.ROLE_ADMIN)))
                    return;

                await foreach (var r in dataProvider.GetAll())
                    await responseStream.WriteAsync(new ExportUsersResponse() { UserRecord = r.Public });
            }
            catch
            {
            }
        }

        public override async Task<RestoreAllDataResponse> RestoreAllData(IAsyncStreamReader<RestoreAllDataRequest> requestStream, ServerCallContext context)
        {
            logger.LogWarning("*** RestoreAllData - Entrance ***");

            RestoreAllDataResponse res = new RestoreAllDataResponse();
            HashSet<Guid> idsLoaded = new HashSet<Guid>();

            await requestStream.MoveNext();
            if (requestStream.Current.RequestOneofCase != RestoreAllDataRequest.RequestOneofOneofCase.Mode)
            {
                logger.LogWarning("*** RestoreAllData - Mode missing ***");
                return res;
            }

            var restoreMode = requestStream.Current.Mode;

            try
            {
                var userToken = ONUserHelper.ParseUser(context.GetHttpContext());
                if (userToken == null)
                {
                    logger.LogWarning("*** RestoreAllData - token bad ***");
                    logger.LogWarning("*** RestoreAllData - jwttoken (" + (context.GetHttpContext()?.Request?.Headers["Authorization"] ?? "empty") + ") ***");
                    return res;
                }
                if (!userToken.Roles.Contains(ONUser.ROLE_BACKUP))
                {
                    logger.LogWarning("*** RestoreAllData - not backup user: (" + String.Join(',', userToken.Roles) + ") ***");
                    logger.LogWarning("*** RestoreAllData - jwttoken (" + (context.GetHttpContext()?.Request?.Headers["Authorization"] ?? "empty") + ") ***");
                    return res;
                }

                await foreach (var r in requestStream.ReadAllAsync())
                {
                    Guid id = r.Record.Data.Public.UserID.ToGuid();
                    idsLoaded.Add(id);

                    try
                    {
                        if (await dataProvider.Exists(id))
                        {
                            if (restoreMode == RestoreAllDataRequest.Types.RestoreMode.MissingOnly)
                            {
                                res.NumUsersSkipped++;
                                continue;
                            }

                            await dataProvider.Save(r.Record.Data);
                            res.NumUsersOverwriten++;
                        }
                        else
                        {
                            await dataProvider.Save(r.Record.Data);
                            res.NumUsersRestored++;
                        }
                    }
                    catch { }
                }

                if (restoreMode == RestoreAllDataRequest.Types.RestoreMode.Wipe)
                {
                    await foreach (var r in dataProvider.GetAll())
                    {
                        Guid id = r.Public.UserID.ToGuid();
                        if (!idsLoaded.Contains(id))
                        {
                            await dataProvider.Delete(id);
                            res.NumUsersWiped++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("*** RestoreAllData - ERROR ***");
                logger.LogWarning($"*** RestoreAllData - ERROR: {ex.Message} ***");
            }

            logger.LogWarning("*** RestoreAllData - Start Reindex ***");

            await dataProvider.ReindexAll();

            logger.LogWarning("*** RestoreAllData - Exit ***");

            return res;
        }
    }
}
