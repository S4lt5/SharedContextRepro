using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using Repro.Models;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Repro.Tests
{
    /// <summary>
    /// This test fails in an interesting way. The server is created twice, and it shares the same context. This was actually not intended when I was creating this.
    /// </summary>
    public class TestBooksInitialData
    {
        public TestServer Server;
        HttpClient Client;
        public TestBooksInitialData()
        {
            Server = new TestServer(new WebHostBuilder().UseContentRoot(System.IO.Directory.GetCurrentDirectory()).UseStartup<Startup>());

            //get a separate server instance to count seed values without concurrency issues
            //Server = new TestServer(TestServer.CreateBuilder().UseStartup<TestStartup>());
            Client = Server.CreateClient();
        }



        [Fact]
        public async Task TestSeedValues()
        {
            var books = await Client.GetObjectFromJsonUrlAsync<ICollection<Book>>("/Books");
            Assert.Equal(books.Count, 23);


        }

        [Fact]
        public async Task TestICanAddABook()
        {
            Book newBook = new Book
            {
                Name = "A Brief History of the Universe"
            };
            var result = await Client.GetResponseFromJsonUrlPost("/Books/Create", newBook);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            var books = await Client.GetObjectFromJsonUrlAsync<ICollection<Book>>("/Books");
            Assert.Equal(24,books.Count);

        }
    }
}
