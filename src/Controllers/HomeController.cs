using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Couchbase;
using Couchbase.Core;

namespace CouchbaseWithVsCode.Controllers
{
    public class HomeController : Controller
    {
        IBucket _bucket;

        public HomeController(IBucket bucket)
        {
            _bucket = bucket;
        }

        public IActionResult Index()
        {
            _bucket.Upsert("thekey", new { welcomeMsg = "Welcome to my app."});
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = _bucket.Get<dynamic>("thekey").Value.welcomeMsg;

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
