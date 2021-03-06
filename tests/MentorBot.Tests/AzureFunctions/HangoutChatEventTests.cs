﻿using System.Net.Http;
using System.Threading.Tasks;

using MentorBot.Functions;
using MentorBot.Functions.Abstract.Services;
using MentorBot.Functions.App;
using MentorBot.Functions.Models.Domains;
using MentorBot.Functions.Models.HangoutsChat;
using MentorBot.Functions.Models.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NSubstitute;

namespace MentorBot.Tests.AzureFunctions
{
    [TestClass]
    [TestCategory("Functions")]
    public class HangoutChatEventTests
    {
        private const string EmptyMsg = @"{ ""type"": ""MESSAGE"", ""eventTime"": ""2019-01-14T15:55:20.120287Z"", ""token"": ""A"", ""message"": {}, ""user"": { ""name"": ""users/1"", ""displayName"": ""A"", ""avatarUrl"": null, ""email"": ""a@b.com"", ""type"": ""HUMAN"" }, ""space"":{ ""name"": ""spaces/c"", ""type"": ""DM"" } }";
        private const string FullMsg = @"{ ""type"": ""MESSAGE"", ""eventTime"": ""2019-01-14T15:55:20.120287Z"", ""token"": ""AAA"", ""message"": { ""name"": ""spaces/q/messages/y"", ""sender"": { ""name"": ""users/1"", ""displayName"": ""Jhon Doe"", ""avatarUrl"": null, ""email"": ""a.b@c.com"", ""type"": ""HUMAN"" }, ""createTime"": ""2019-01-14T15:55:20.120287Z"", ""text"": ""What is this?"", ""thread"": { ""name"": ""spaces/q/threads/y"", ""retentionSettings"": { ""state"": ""PERMANENT"" } }, ""space"": { ""name"": ""spaces/q"", ""type"": ""DM"" }, ""argumentText"": ""What is this"" }, ""user"": { ""name"": ""users/1"", ""displayName"": ""Jhon Doe"", ""avatarUrl"": null, ""email"": ""a.b@c.com"", ""type"": ""HUMAN"" }, ""space"": { ""name"": ""spaces/q"", ""type"": ""DM"" }}";

        [TestMethod]
        public async Task RunAsync_ShouldCheckToken()
        {
            var documentClientService = Substitute.For<IDocumentClientService>();
            var document = Substitute.For<IDocument<Message>>();
            var hangoutsChatService = Substitute.For<IHangoutsChatService>();
            var options = new GoogleCloudOptions("А", "B", "C", "D");
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();
            var message = new HttpRequestMessage { Content = new StringContent(EmptyMsg) };

            ServiceLocator.DefaultInstance.BuildServiceProviderWithDescriptors(
                new ServiceDescriptor(typeof(IDocumentClientService), documentClientService),
                new ServiceDescriptor(typeof(IHangoutsChatService), hangoutsChatService),
                new ServiceDescriptor(typeof(GoogleCloudOptions), options));

            var result = await HangoutChatEvent.RunAsync(message, logger);

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task RunAsync_ShouldCallService()
        {
            var documentClientService = Substitute.For<IDocumentClientService>();
            var document = Substitute.For<IDocument<Message>>();
            var hangoutsChatService = Substitute.For<IHangoutsChatService>();
            var options = new GoogleCloudOptions("AAA", "B", "C", "D");
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();
            var requestMessage = new HttpRequestMessage { Content = new StringContent(FullMsg) };
            var message = new Message { Output = new ChatEventResult("OK") };

            ServiceLocator.DefaultInstance.BuildServiceProviderWithDescriptors(
                new ServiceDescriptor(typeof(IDocumentClientService), documentClientService),
                new ServiceDescriptor(typeof(IHangoutsChatService), hangoutsChatService),
                new ServiceDescriptor(typeof(GoogleCloudOptions), options));

            hangoutsChatService
                .BasicAsync(Arg.Is<ChatEvent>(it =>
                    it.Message.Text == "What is this?" &&
                    it.Message.Name == "spaces/q/messages/y"))
                .Returns(message);

            var result = await HangoutChatEvent.RunAsync(requestMessage, logger);
            var resultOutput = (result as JsonResult).Value as ChatEventResult;

            Assert.AreEqual(resultOutput.Text, "OK");
        }

#pragma warning disable CS4014

        [Ignore]
        [TestMethod]
        public async Task RunAsync_ShouldSaveInDb()
        {
            var documentClientService = Substitute.For<IDocumentClientService>();
            var document = Substitute.For<IDocument<Message>>();
            var hangoutsChatService = Substitute.For<IHangoutsChatService>();
            var options = new GoogleCloudOptions("AAA", "B", "C", "D");
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();
            var requestMessage = new HttpRequestMessage { Content = new StringContent(FullMsg) };
            var message = new Message();

            ServiceLocator.DefaultInstance.BuildServiceProviderWithDescriptors(
                new ServiceDescriptor(typeof(IDocumentClientService), documentClientService),
                new ServiceDescriptor(typeof(IHangoutsChatService), hangoutsChatService),
                new ServiceDescriptor(typeof(GoogleCloudOptions), options));

            documentClientService.IsConnected.Returns(true);
            documentClientService.Get<Message>("mentorbot", "messages").Returns(document);
            hangoutsChatService.BasicAsync(null).ReturnsForAnyArgs(message);

            await HangoutChatEvent.RunAsync(requestMessage, logger);

            document.Received().AddAsync(message);
        }

#pragma warning restore CS4014
    }
}
