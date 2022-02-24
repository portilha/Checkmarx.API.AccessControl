using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Checkmarx.API.AccessControl.Tests
{
    [TestClass]
    public class ACTests
    {
       private static AccessControlClient _accessControlClient;

        public static IConfigurationRoot Configuration { get; private set; }


        [ClassInitialize]
        public static void InitializeTest(TestContext testContext)
        {
            // TODO REMOVE
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<ACTests>();

            var httpClient = new HttpClient();

            _accessControlClient = new AccessControlClient(httpClient);
        }

        [TestMethod]
        public void ListCheckmarxUsers()
        {


            var roles = _accessControlClient.RolesAllAsync().Result.ToDictionary(x => x.Id);
            var teams = _accessControlClient.TeamsAllAsync().Result.ToDictionary(x => x.Id);

            foreach (var user in _accessControlClient.GetAllUsersDetailsAsync().Result)
            {
                if (user.Email.EndsWith("@checkmarx.com"))
                {
                    Trace.WriteLine(user.Email + string.Join(";", user.TeamIds.Select(x => teams[x].FullName)) + " " + user.LastLoginDate);

                    foreach (var role in user.RoleIds.Select(x => roles[x].Name))
                    {
                        Trace.WriteLine("+ " + role);
                    }

                    _accessControlClient.UpdateUserDetails(user.Id,
                        new UpdateUserModel
                        {
                            ExpirationDate = new DateTimeOffset(new DateTime(2025, 10, 01))
                        }).Wait();
                }
            }
        }


        [TestMethod]
        public void UpdateExpirationDateTest()
        {

            foreach (var user in _accessControlClient.GetAllUsersDetailsAsync().Result.Where(x => x.Active))
            {
                Trace.WriteLine(user.UserName + " " + user.ExpirationDate);

                var userUpdate = GetUpdateUserModel(user);

                userUpdate.ExpirationDate = new DateTimeOffset(new DateTime(2025, 1, 10));

                _accessControlClient.UpdateUserDetails(user.Id, userUpdate).Wait();
            }
        }

        private static UpdateUserModel GetUpdateUserModel(UserViewModel user)
        {
            return new UpdateUserModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                AllowedIpList = user.AllowedIpList,
                CellPhoneNumber = user.CellPhoneNumber,
                Country = user.Country,
                Email = user.Email,
                JobTitle = user.JobTitle,
                LocaleId = user.LocaleId,
                Other = user.Other,
                RoleIds = user.RoleIds,
                TeamIds = user.TeamIds,
                PhoneNumber = user.PhoneNumber,
                Active = user.Active,
                ExpirationDate = user.ExpirationDate
            };
        }

        [TestMethod]
        public void ResetPasswordTest()
        {
            var userID = _accessControlClient.GetAllUsersDetailsAsync().Result.First(x => x.UserName == "pedro.portilha@checkmarx.com").Id;

            var result = _accessControlClient.ResetPassword2Async(userID).Result;

            Trace.WriteLine(result.GeneratedPassword);

            Assert.IsNotNull(result.GeneratedPassword);
        }


        [TestMethod]
        public void CreateUserTest()
        {
            ICollection<int> cxTamRoles = new int[] {
                _accessControlClient.RolesAllAsync().Result.First(x => x.Name == "SAST Admin").Id
            };

            ICollection<int> cxTeamIds = new int[] {
                _accessControlClient.TeamsAllAsync().Result.First(x => x.FullName == "/CxServer").Id
            };

            int localeID = _accessControlClient.SystemLocalesAsync().Result.First(x => x.Code == "en-US").Id;

            CreateUserModel user = new CreateUserModel
            {
                FirstName = "firstname",
                LastName = "lastname",
                UserName = "email@checkmarx.com",
                Email = "email@checkmarx.com",
                Password = "randomPassword",
                ExpirationDate = DateTimeOffset.UtcNow + TimeSpan.FromDays(1000),
                Active = true,

                Country = "Portugal",
                JobTitle = "The World Greatest",

                AuthenticationProviderId = _accessControlClient.AuthenticationProvidersAsync().Result.First(X => X.Name == "Application").Id, // Application User

                LocaleId = localeID,
                RoleIds = cxTamRoles,
                TeamIds = cxTeamIds,

            };

            _accessControlClient.CreatesNewUser(user).Wait();
        }

        [TestMethod]
        public void ListTeamsTest()
        {
            foreach (var item in _accessControlClient.TeamsAllAsync().Result)
            {
                Trace.WriteLine($"{item.Id} = {item.FullName}");
            }
        }

        [TestMethod]
        public void ListLocalsTest()
        {
            foreach (var item in _accessControlClient.SystemLocalesAsync().Result)
            {
                Trace.WriteLine($"{item.Id} = {item.Code} = {item.DisplayName}");
            }
        }

        [TestMethod]
        public void ListAuthTest()
        {
            foreach (var item in _accessControlClient.AuthenticationProvidersAsync().Result)
            {
                Trace.WriteLine($"{item.Id} = {item.Name} = {item.ProviderType}");
            }
        }



        [TestMethod]
        public void ListRolesTest()
        {
            foreach (var item in _accessControlClient.RolesAllAsync().Result)
            {
                Trace.WriteLine($"{item.Id} = {item.Name}");
            }
        }
    }
}
