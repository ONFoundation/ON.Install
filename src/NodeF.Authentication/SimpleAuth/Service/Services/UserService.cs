using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NodeF.Authentication.SimpleAuth.Service.Data;
using NodeF.Fragments.Authentcation;
using NodeF.Fragments.Generic;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NodeF.Authentication.SimpleAuth.Service.Services
{
    public class UserService : UserInterface.UserInterfaceBase
    {
        private readonly ILogger<ServiceOpsService> logger;
        private readonly SigningCredentials creds;
        private readonly IUserDataProvider dataProvider;
        private static readonly HashAlgorithm hasher = new SHA256Managed();
        private static readonly RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        public UserService(ILogger<ServiceOpsService> logger, IUserDataProvider dataProvider)
        {
            this.logger = logger;
            this.dataProvider = dataProvider;

            creds = new SigningCredentials(JwtValidatorMiddleware.GetPrivateKey(), SecurityAlgorithms.EcdsaSha256);
        }

        public override async Task<AuthenticatUserResponse> AuthenticatUser(AuthenticatUserRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                return new AuthenticatUserResponse();

            var user = await dataProvider.GetByLogin(request.UserName);
            if (user == null)
                return new AuthenticatUserResponse();

            var hash = ComputeSaltedHash(request.Password, user.Private.PasswordSalt.Span);
            if (!CryptographicOperations.FixedTimeEquals(user.Private.PasswordHash.Span, hash))
                return new AuthenticatUserResponse();

            return new AuthenticatUserResponse()
            {
                BearerToken = GenerateToken(user)
            };
        }

        public override async Task<ChangeOwnPasswordResponse> ChangeOwnPassword(ChangeOwnPasswordRequest request, ServerCallContext context)
        {
            try
            {
                var userToken = context.GetHttpContext().User as NodeUser;
                if (userToken == null)
                    return new ChangeOwnPasswordResponse { Error = ChangeOwnPasswordResponse.Types.ErrorType.UnknownError };

                var record = await dataProvider.GetById(userToken.Id);
                if (record == null)
                    return new ChangeOwnPasswordResponse { Error = ChangeOwnPasswordResponse.Types.ErrorType.UnknownError };

                var hash = ComputeSaltedHash(request.OldPassword, record.Private.PasswordSalt.Span);
                if (!CryptographicOperations.FixedTimeEquals(record.Private.PasswordHash.Span, hash))
                    return new ChangeOwnPasswordResponse { Error = ChangeOwnPasswordResponse.Types.ErrorType.BadOldPassword };

                byte[] salt = new byte[16];
                rngCsp.GetBytes(salt);
                record.Private.PasswordSalt = Google.Protobuf.ByteString.CopyFrom(salt);
                record.Private.PasswordHash = Google.Protobuf.ByteString.CopyFrom(ComputeSaltedHash(request.NewPassword, salt));

                record.Private.ModifiedOnUTC = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);

                await dataProvider.Save(record);

                return new ChangeOwnPasswordResponse { Error = ChangeOwnPasswordResponse.Types.ErrorType.NoError };
            }
            catch
            {
                return new ChangeOwnPasswordResponse { Error = ChangeOwnPasswordResponse.Types.ErrorType.UnknownError };
            }
        }

        public override async Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            var user = request.Record;
            if (user == null)
                return new CreateUserResponse()
                {
                    Error = CreateUserResponse.Types.ErrorType.UnknownError
                };

            byte[] salt = new byte[16];
            rngCsp.GetBytes(salt);
            user.Private.PasswordSalt = Google.Protobuf.ByteString.CopyFrom(salt);
            user.Private.PasswordHash = Google.Protobuf.ByteString.CopyFrom(ComputeSaltedHash(request.Password, salt));
            user.Private.CreatedOnUTC = user.Private.ModifiedOnUTC = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);

            if (!IsValid(user))
                return new CreateUserResponse
                {
                    Error = CreateUserResponse.Types.ErrorType.UnknownError
                };

            if (await dataProvider.Exists(request.Record.Public.UserName))
                return new CreateUserResponse
                {
                    Error = CreateUserResponse.Types.ErrorType.UserNameTaken
                };

            var res = await dataProvider.Create(user);
            if (!res)
                return new CreateUserResponse
                {
                    Error = CreateUserResponse.Types.ErrorType.UnknownError
                };

            return new CreateUserResponse
            {
                BearerToken = GenerateToken(user)
            };
        }

        public override async Task<GetOwnUserResponse> GetOwnUser(GetOwnUserRequest request, ServerCallContext context)
        {
            var userToken = context.GetHttpContext().User as NodeUser;
            if (userToken == null)
                return new GetOwnUserResponse();

            return new GetOwnUserResponse
            {
                Record = await dataProvider.GetById(userToken.Id)
            };
        }

        public override async Task<ModifyOwnUserResponse> ModifyOwnUser(ModifyOwnUserRequest request, ServerCallContext context)
        {
            try
            {
                var userToken = context.GetHttpContext().User as NodeUser;
                if (userToken == null)
                    return new ModifyOwnUserResponse() { Error = "No user token specified" };

                var record = await dataProvider.GetById(userToken.Id);
                if (record == null)
                    return new ModifyOwnUserResponse() { Error = "User not found" };

                if (!IsDisplayNameValid(request.DisplayName))
                    return new ModifyOwnUserResponse() { Error = "Display Name not valid" };

                record.Private.ModifiedOnUTC = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
                record.Public.DisplayName = request.DisplayName;

                record.Public.Identities.Clear();
                record.Public.Identities.AddRange(request.Identities);

                record.Private.Emails.Clear();
                record.Private.Emails.AddRange(request.Emails);

                await dataProvider.Save(record);

                return new ModifyOwnUserResponse();
            }
            catch
            {
                return new ModifyOwnUserResponse() { Error = "Unknown error" };
            }
        }

        private bool IsValid(UserRecord user)
        {
            if (new Guid(user.Public.UserID.Span) == Guid.Empty)
                return false;

            user.Public.DisplayName = user.Public.DisplayName?.Trim() ?? "";
            if (!IsDisplayNameValid(user.Public.DisplayName))
                return false;


            user.Public.UserName = user.Public.UserName?.Trim() ?? "";
            if (!IsUserNameValid(user.Public.UserName))
                return false;

            return true;
        }

        private bool IsDisplayNameValid(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            if (displayName.Length < 4 || displayName.Length > 20)
                return false;

            return true;
        }

        private bool IsUserNameValid(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            if (userName.Length < 4 || userName.Length > 20)
                return false;

            var regex = new Regex(@"^[a-zA-Z0-9]+$");
            if (!regex.IsMatch(userName))
                return false;

            return true;
        }

        private byte[] ComputeSaltedHash(string plainText, ReadOnlySpan<byte> salt)
        {
            return ComputeSaltedHash(Encoding.UTF8.GetBytes(plainText), salt);
        }

        private byte[] ComputeSaltedHash(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> salt)
        {
            byte[] plainTextWithSaltBytes = new byte[plainText.Length + salt.Length];

            plainText.CopyTo(plainTextWithSaltBytes.AsSpan());
            salt.CopyTo(plainTextWithSaltBytes.AsSpan(plainText.Length));

            return hasher.ComputeHash(plainTextWithSaltBytes);
        }

        private string GenerateToken(UserRecord user)
        {
            var node = new NodeUser()
            {
                Id = new Guid(user.Public.UserID.Span),
                DisplayName = user.Public.DisplayName,
            };
            node.Idents.AddRange(user.Public.Identities);

            return GenerateToken(node);
        }

        private string GenerateToken(NodeUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds
            };

            tokenDescriptor.Claims = new Dictionary<string, object>();

            foreach (var c in user.ToClaims())
                tokenDescriptor.Claims.Add(c.Type, c.Value);

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}