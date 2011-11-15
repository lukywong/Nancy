using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.ModelBinding;

namespace TryOutNancy
{
    public class LoginModule : NancyModule
    {
        public LoginModule()
        {

            Post["/login"] = x =>
            {
                LoginInfo login = this.Bind();

                return login.IsValid()
                    ? "Success"
                    : "Failure";
            };

            Get["/"] = x =>
            {
                return View["login"];
            };
        }
    }

    public class LoginInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public bool IsValid()
        {
            return Username == "user" && Password == "pass";
        }
    }
}