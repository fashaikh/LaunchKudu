using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchKudu
{
    public static class EasyAuth
    {
        public static async Task<AuthenticationResult> AccessToken()
        {
            //Get access token:   
            // To call a Data Catalog REST operation, create an instance of AuthenticationContext and call AcquireToken  
            // AuthenticationContext is part of the Active Directory Authentication Library NuGet package  
            // To install the Active Directory Authentication Library NuGet package in Visual Studio,   
            //  run "Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory Version 2.19.208020213" from the nuget Package Manager Console.  

            //Guid for Resource Uri for wawsobserver  
            string resourceUri = "d1abfd91-e19c-426e-802f-a6c55421a5ef";

            //To learn how to register a client app and get a Client ID, see https://msdn.microsoft.com/en-us/library/azure/mt403303.aspx#clientID     
            string clientId = "5287de56-2965-4224-bee9-256618a3931c";

            //A redirect uri gives AAD more details about the specific application that it will authenticate.  
            //Since a client app does not have an external service to redirect to, this Uri is the standard placeholder for a client app.  
            string redirectUri = "https://login.live.com/oauth20_desktop.srf";

            // Create an instance of AuthenticationContext to acquire an Azure access token  
            // OAuth2 authority Uri  
            string authorityUri = "https://login.windows.net/common/oauth2/authorize";
            AuthenticationContext authContext = new AuthenticationContext(authorityUri);
             UserCredential uc = new UserCredential();

            // Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint  
            //  AcquireToken takes a Client Id that Azure AD creates when you register your client app.  
            return await authContext.AcquireTokenAsync(resourceUri, clientId, uc);
        }

    }
}
