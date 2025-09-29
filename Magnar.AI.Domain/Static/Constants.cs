using Magnar.AI.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Magnar.AI.Static;

public static class Constants
{
    public static class Errors
    {
        public const string CheckReCaptcha = "Errors.CheckReCaptcha";
        public const string UserEmailNotConfirmed = "Errors.UserEmailNotConfirmed";
        public const string InvalidUsernameOrPassword = "Errors.InvalidUsernameOrPassword";
        public const string UserAlreadyExists = "Errors.UserExists";
        public const string UsernameMustBeDifferentThanEmail = "Errors.UsernameMustBeDifferentThanEmail";
        public const string Unauthorized = "Errors.Unauthorized";
        public const string UserNotFound = "Errors.UserNotFound";
        public const string InvalidEmail = "Errors.InvalidEmail.";
        public const string IncorrectCurrentPassword = "Errors.IncorrectCurrentPassword";
        public const string InvalidResetPasswordToken = "Errors.InvalidResetPasswordToken";
        public const string InvalidEmailConfirmToken = "Errors.InvalidEmailConfirmToken";
        public const string RoleDoesNotExist = "Errors.RoleDoesNotExist";
        public const string UserNotActive = "Errors.UserNotActive";
        public const string NotFound = "Errors.NotFound";
        public const string CannotCreateAdmin = "Errors.CannotCreateAdmin";
        public const string CannotUpdateRoleToAdmin = "Errors.CannotUpdateRoleToAdmin";
        public const string UserAssignedToThisCompany = "Errors.UserAssignedToThisCompany";
        public const string CannotResetPassword = "Errors.CannotResetPassword";
        public const string UserNotDeleted = "Errors.UserNotDeleted";
        public const string CannotPerformAISearch = "Errors.CannotPerformAISearch";
        public const string ErrorOccured = "Errors.ErrorOccured";
        public const string OpenAiAccess = "Errors.OpenAiAccess";
        public const string MissingOpenAiApiKey = "Errors.MissingOpenAiApiKey";
        public const string CannotGenerateDashboard = "Errors.CannotGenerateDashboard";
        public const string GenerateDashboardError = "Errors.GenerateDashboardError";
        public const string VectorSearchNotEnabled = "Errors.VectorSearchNotEnabled";
        public const string DatabaseInitialization = "Errors.DatabaseInitialization";
        public const string NoDefaultConnectionConfigured = "Errors.NoDefaultConnectionConfigured";
        public const string ConnectionSuccessful = "Errors.ConnectionSuccessful";
        public const string ConnectionFailed = "Errors.ConnectionFailed";
        public const string CannotGenerateDashboardUpdateSchema = "Errors.CannotGenerateDashboardUpdateSchema";
        public const string GenerateSqlError = "Errors.GenerateSqlError";
        public const string ExecuteSqlError = "Errors.ExecuteSqlError";
    }

    public static class Common
    {
        public const string System = "System";
    }

    public static class SigningCredentials
    {
        public const string PrivateKey = @"MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCuz3Z1MIKPW9w/3plpXzrA1BQhEXPNzffbVFdpLwYnfAXf9v0hXvhgHpfp0i5AhRebMDffB7pDa54iZp965nyjsDJyGgvPBHokTF4NpWEDHPw3nudUh3xkOu4/AET1JED25fIPQ9OzwxTV4KGXNP1LDxdiNbRACE3j8G4sWnMozqA/k+2Wfbc68l9/HR+MYXab8PmN+QVKD/Bi9TGkJW6J7VAR6W1nKcdHvL/9NVhhFSs3oODxG7i7R6dwSOBKosGnZdxEESfEWj10P4/FMGBMMfYONX/0z8YFxnORyJFuezCrPk9UwD1IApwi6YifOqbnpCpB16t7FrmkZImbjbaJAgMBAAECggEBAKCi1ijkNeWEtUUfyXux3ayadhDZF8BT9+Jdg6GNa6tW5ZMkRQGoYrb5BgIAeS4i30llKsJROJGB0COuh/iI2poUbJa7ZoSKia7XWkpR4W7Z8M8vv0XG5sC4Anx0Q+m8sdHwBdqPKnfy2S+cpwDq2aNir8s4rHV27BR8uNEbIN2qVRWmgrVaVMFnMx/4dRyJuZlbJzGyJiU20zxiNla4wlD/qNp+qlKD5F818yDPVl4pUNElS0elZ05tWUZ6tk7FCvIyh9t/KyfpuA/+7tve2PuTl8tVANzi1+q8HCwULDm6aZpJbRQBnxv5rql2lqtcS+xbgYTe6zHnLb+sKBGL3m0CgYEA3YFTv3YpSSmz0GOdkVVddQIIsjonRml6Ysswh7Zj0XAK784gufQiEjM7TLjt0chd9EPZZ4KFcIbvcTl/wrRTUEluURrQ7pE1t+9fTt9foeiDwHhJcaFzkiR5mJmCqqzBGOddY+/tlC3VXx7jD0D98/UMYstYC+uC/U74lkxx1I8CgYEAygiSAgLozUEAyH05JecjZw+BH+5XVYSaop3jN0h6V+2Cc5sGzpLHZ6rH7Z7WgMD0q2cfLBRGQnma/Ws8s6qOQ2wzewiBnslix6WRxkydidNaRA9mZjvp46AgNGLR0CsFv5UJxM+mAkqafc6MXgszZ8kGrSK6pF12R1Vbuu9uP2cCgYArblovZkm+ELEzIPUaM/S5Jobx+zkMM05N5K1FTtvPivp5/p6oF1g+3VC5QGFRCspxRIRyKUNkxvBX8QA9+wGC8oLw4CMrQ8mWoRa87ktgAakjqfIsl42DkCdPZnoaYPkTmvnSyI56yWnW1sWKjiG9HcUp8dj3rVvnsv5G6gI/vQKBgQC0dqtVOJwSm7nDXHerr6cj6+l/SDqinOwzzaykOQ1vdSxNb3DJHLsZkqc7TeJ05+alJFvn18RapJ0ZOKzkH7kr6H6jq5l86I5fnzlzVAR0lGHQ4aCqOWJWfTXHFW4s8AEWfF5ZetHiwhj1v4YKix7D2gEorhjhsMpyNYDWngBwiQKBgAmSbs81B/nld1YEwMNC/I7huNKd7TvLA49nequm4fsddhtOoe6qagfRTmeyPJpI2Sm+1fuRA5auQiKwpGtyYsVFiTZ+BaWGZOH6rgrkrsnWKMDDOh0pR2BLNG1jkwYGaJYiIksN5W23LVWH7dSXevfLGgvA/oroorG1ApsU3XIn";
    }

    public static class Dashboards
    {
        public const string DynamicQuery = "DynamicQuery";
        public const string DynamicSqlDataSource = "DynamicSqlDataSource";
        public const string Category = "Category";
        public const string Value = "Value";
    }

    public static class ClientApp
    {
        public const string DistPath = "ClientApp/dist";

        public const string RootPath = "ClientApp";

        public const string ClientIndexFileName = "index.html";
    }

    public readonly struct RouteParameters
    {
        public const string WorkspaceParameterName = "WorkspaceId";
    }

    public static class Localization
    {
        public const string DirectoryPath = "Resources";
        public const string DefaultCulture = "en-US";
        public const string RecoverPasswordEmailBody = "RecoverPasswordBody";
        public const string RecoverPasswordEmailSubject = "RecoverPasswordSubject";
        public const string EmailConfirmationBody = "EmailConfirmationBody";
        public const string EmailConfirmationSubject = "EmailConfirmationSubject";
    }

    public static class Configuration
    {
        public static class Sections
        {
            public const string IdentityApi = "IdentityApiConfiguration";
            public const string IdentityServerClients = "IdentityApiConfiguration:Clients";
            public const string Email = "NotificationConfiguration:EmailConfiguration";
            public const string Cors = "CorsConfiguration";
            public const string Swagger = "SwaggerConfiguration";
            public const string OData = "ODataConfiguration";
            public const string ReCaptcha = "ReCaptchaConfiguration";
            public const string OpenAIConfiguration = "OpenAIConfiguration";
            public const string Urls = "UrlsConfiguration";
            public const string VectorConfiguration = "VectorConfiguration";
            public const string ApiKeysConfiguration = "ApiKeys";
        }

        public static class Keys
        {
            public const string DefaultExpirationInSeconds = "CacheConfiguration:DefaultExpirationInSeconds";
        }
    }

    public static class ExceptionMessages
    {
        public const string ValidationError = "ExceptionMessages.ValidationError";
        public const string TransactionAlreadyCreated = "A transaction to the database is already established";
    }

    public static class Folders
    {
        public const string Assets = "Assets";
        public const string Prompts = "Prompts";
        public const string DashboardPrompts = "Dashboard";
        public const string GenerateSql = "GenerateSql";
        public const string SystemPrompts = "SystemPrompts";
        public const string UserPrompts = "UserPrompts";
    }

    public static class ValidationMessages
    {
        public const string RequiredField = "ValidationMessages.RequiredField";
        public const string PromptRequired = "ValidationMessages.PromptRequired";
        public const string MaxTop10 = "ValidationMessages.MaxTop10";
        public const string CannotHaveMultipleDefaultConnections = "ValidationMessages.CannotHaveMultipleDefaultConnections";
        public const string FunctionNameExist = "ValidationMessages.FunctionNameExist";
        public const string FunctionNameFormat = "ValidationMessages.FunctionNameFormat";

        public static class Identity
        {
            public const string InvalidEmail = "ValidationMessages.Identity.InvalidEmail";
            public const string PasswordRequired = "ValidationMessages.Identity.PasswordRequired";
            public const string CurrentPasswordRequired = "ValidationMessages.Identity.CurrentPasswordRequired";
            public const string NewPasswordRequired = "ValidationMessages.Identity.NewPasswordRequired";
            public const string InvalidResetToken = "ValidationMessages.Identity.InvalidResetToken";
            public const string InvalidEmailoken = "ValidationMessages.Identity.InvalidEmailoken";
            public const string InvalidNewPassword = "ValidationMessages.Identity.InvalidNewPassword";
            public const string UsernameRequired = "ValidationMessages.Identity.UsernameRequired";
            public const string FirstNameRequired = "ValidationMessages.Identity.FirstNameRequired";
            public const string LastNameRequired = "ValidationMessages.Identity.LastNameRequired";
            public const string RoleRequired = "ValidationMessages.Identity.RoleRequired";
        }
    }

    public static class DataProtector
    {
        public const string Purpose = "B1122BCF-FA8E-4E02-8078-B367976D1CAA";
    }

    public static class PlaceHolders
    {
        public const string BaseUrl = "BASEURL";
        public const string Token = "TOKEN";
        public const string UserId = "USERID";
    }

    public static class TokenTypes
    {
        public const string PasswordReset = UserManager<ApplicationUser>.ResetPasswordTokenPurpose;
        public const string EmailConfirm = UserManager<ApplicationUser>.ConfirmEmailTokenPurpose;
    }

    public static class Database
    {
        public const string DefaultConnectionString = "Default";

        public static class TableNames
        {
            public const string Users = "Users";
            public const string UserClaims = "UserClaims";
            public const string UserRoles = "UserRoles";
            public const string UserLogins = "UserLogins";
            public const string UserTokens = "UserTokens";
            public const string Roles = "Roles";
            public const string RoleClaims = "RoleClaims";
            public const string DataProtectionKeys = "DataProtectionKeys";
        }

        public static class Schemas
        {
            public const string Default = "dbo";
            public const string Identity = "idn";
        }
    }

    public static class IdentityApi
    {
        public static class Endpoints
        {
            public static class Routes
            {
                public const string AccessToken = "connect/token";
            }

            public static class Parameters
            {
                public const string GrantType = "grant_type";
                public const string ClientId = "client_id";
                public const string ClientSecret = "client_secret";
                public const string Scope = "scope";
                public const string RefreshToken = "refresh_token";
                public const string UserName = "username";
                public const string Password = "password";
            }
        }

        public static class ApiResourceSecret
        {
            public const string WebApiKey = "SvB?2G)Tm?WI;6E'Y,%.nJ7-z?#xDqV<B`v@Iq<s(S*nl.4_:7PNFc9n(LV*%8";
        }

        public static class ApiResourceNames
        {
            public const string WebApi = "WebApi";

            public const string Custom = "custom";
        }

        public static class ApiScopeNames
        {
            public const string Read = "read";
            public const string Write = "write";
            public const string Modify = "modify";
            public const string Full = "full";
        }

        public static class ApiScopeDescriptions
        {
            public const string Full = "Gives access to all protected endpoints";
            public const string Read = "Gives access to [Get] endpoints";
            public const string Write = "Gives access to [Post] endpoints";
            public const string Modify = "Gives access to [Put]/[Delete] endpoints";
        }

        public static class Clients
        {
            public static class Api
            {
                public const string DefaultScope = "full offline_access";
                public const string Id = "magnar.ai.api.d2944c22-84bd-40a7-9d86-76324a1c9fc5";
                public const string Secret = "SvB?2G)Tm?WI;6E'Y,%.nJ7-z?#xDqV<B`v@Iq<s(S*nl.4_:7PNFc9n(LV*%8";

                public static class GrantTypes
                {
                    public const string Password = "password";
                    public const string ClientCredentials = "client_credentials";
                    public const string RefreshToken = "refresh_token";
                    public const string ApiKey = "api_key";
                }
            }
        }

        public static class SigningCredentials
        {
            public const string PrivateKey = @"MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCuz3Z1MIKPW9w/3plpXzrA1BQhEXPNzffbVFdpLwYnfAXf9v0hXvhgHpfp0i5AhRebMDffB7pDa54iZp965nyjsDJyGgvPBHokTF4NpWEDHPw3nudUh3xkOu4/AET1JED25fIPQ9OzwxTV4KGXNP1LDxdiNbRACE3j8G4sWnMozqA/k+2Wfbc68l9/HR+MYXab8PmN+QVKD/Bi9TGkJW6J7VAR6W1nKcdHvL/9NVhhFSs3oODxG7i7R6dwSOBKosGnZdxEESfEWj10P4/FMGBMMfYONX/0z8YFxnORyJFuezCrPk9UwD1IApwi6YifOqbnpCpB16t7FrmkZImbjbaJAgMBAAECggEBAKCi1ijkNeWEtUUfyXux3ayadhDZF8BT9+Jdg6GNa6tW5ZMkRQGoYrb5BgIAeS4i30llKsJROJGB0COuh/iI2poUbJa7ZoSKia7XWkpR4W7Z8M8vv0XG5sC4Anx0Q+m8sdHwBdqPKnfy2S+cpwDq2aNir8s4rHV27BR8uNEbIN2qVRWmgrVaVMFnMx/4dRyJuZlbJzGyJiU20zxiNla4wlD/qNp+qlKD5F818yDPVl4pUNElS0elZ05tWUZ6tk7FCvIyh9t/KyfpuA/+7tve2PuTl8tVANzi1+q8HCwULDm6aZpJbRQBnxv5rql2lqtcS+xbgYTe6zHnLb+sKBGL3m0CgYEA3YFTv3YpSSmz0GOdkVVddQIIsjonRml6Ysswh7Zj0XAK784gufQiEjM7TLjt0chd9EPZZ4KFcIbvcTl/wrRTUEluURrQ7pE1t+9fTt9foeiDwHhJcaFzkiR5mJmCqqzBGOddY+/tlC3VXx7jD0D98/UMYstYC+uC/U74lkxx1I8CgYEAygiSAgLozUEAyH05JecjZw+BH+5XVYSaop3jN0h6V+2Cc5sGzpLHZ6rH7Z7WgMD0q2cfLBRGQnma/Ws8s6qOQ2wzewiBnslix6WRxkydidNaRA9mZjvp46AgNGLR0CsFv5UJxM+mAkqafc6MXgszZ8kGrSK6pF12R1Vbuu9uP2cCgYArblovZkm+ELEzIPUaM/S5Jobx+zkMM05N5K1FTtvPivp5/p6oF1g+3VC5QGFRCspxRIRyKUNkxvBX8QA9+wGC8oLw4CMrQ8mWoRa87ktgAakjqfIsl42DkCdPZnoaYPkTmvnSyI56yWnW1sWKjiG9HcUp8dj3rVvnsv5G6gI/vQKBgQC0dqtVOJwSm7nDXHerr6cj6+l/SDqinOwzzaykOQ1vdSxNb3DJHLsZkqc7TeJ05+alJFvn18RapJ0ZOKzkH7kr6H6jq5l86I5fnzlzVAR0lGHQ4aCqOWJWfTXHFW4s8AEWfF5ZetHiwhj1v4YKix7D2gEorhjhsMpyNYDWngBwiQKBgAmSbs81B/nld1YEwMNC/I7huNKd7TvLA49nequm4fsddhtOoe6qagfRTmeyPJpI2Sm+1fuRA5auQiKwpGtyYsVFiTZ+BaWGZOH6rgrkrsnWKMDDOh0pR2BLNG1jkwYGaJYiIksN5W23LVWH7dSXevfLGgvA/oroorG1ApsU3XIn";
        }

        public static class ApiClaims
        {
            public const string Username = "username";
            public const string Email = "email";
        }
    }

    public static class Seeds
    {
        public static class DefaultUser
        {
            public const string Username = "pilot";
            public const string Email = "pilot@gmail.com";
            public const string FirstName = "magnar";
            public const string LastName = "systems";
            public const string Password = "@Magnar.123";
        }
    }

    public static class ReCaptcha
    {
        public static class Parameters
        {
            public const string Secret = "secret";
            public const string Response = "response";
        }
    }

    public static class KernelFunctionNames
    {
        public const string DefaultQueryGenerator = "default_query_generator";
    }

    public static class KernelPluginsNames
    {
        public const string DefaultPlugin = "DefaultPlugin";
    }

    public static class AiModels 
    {
        public const string Gpt4o = "gpt-4o";
        public const string Gpt41 = "gpt-4.1";
        public const string Gpt4 = "gpt-4";
        public const string Gpt35 = "gpt-3.5";
        public const string Gpt4omini = "gpt-4o-mini";

        public const string Davinci = "davinci";
        public const string Curie = "curie";
        public const string Babbage = "babbage";
        public const string Ada = "ada";
    }
}