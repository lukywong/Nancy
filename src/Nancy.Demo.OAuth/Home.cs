namespace Nancy.Demo.OAuth
{
    using System;
    using Authentication.Forms;
    using Extensions;
    using Nancy;

    public class Home : NancyModule
    {
        public Home()
        {
            Get["/"] = parameters => {
                return "Up and running";
            };

            Get["/logout"] = x =>
            {
                return this.LogoutAndRedirect("~/");
            };

            Get["/login"] = x =>
            {
                return View["login"];
            };

            Post["/login"] = x =>
            {
                var userGuid = UserDatabase.ValidateUser((string)this.Request.Form.Username, (string)this.Request.Form.Password);

                if (userGuid == null)
                {
                    return Context.GetRedirect("~/login?error=true&username=" + (string)this.Request.Form.Username);
                }

                DateTime? expiry = null;
                if (this.Request.Form.RememberMe.HasValue)
                {
                    expiry = DateTime.Now.AddDays(7);
                }

                return this.LoginAndRedirect(userGuid.Value, expiry);
            };
        }
    }
}