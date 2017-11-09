#FIFA.com Authentication webapp
--------------------------------------------------

INTEGRATION GUIDE:
https://fifadigital.atlassian.net/wiki/spaces/FDCP/pages/107348028/B2C+integration+guide

WORKFLOW:
The FIFA.com's user will click on the login button and is redirected to the MS B2C login page.
After the user has inserted his credential and confirmed, the MS B2C system will redirect to the url specified in the parameters as redirect_uri.
At the redirect_uri will respond our application. The application will receive the "code" parameter (representing the authorisation code) 
and the "state" parameter (wich is the base64 starting url).
By using the code parameter, the app must be able to get the authentication tokens and with that, it must call the Microsoft FAN API in order to retrieve the user's data.
Once the User's data have been collected, the application must set a "FIFACom" cookie on the domain ".qa.fifa.com". 
After the cookie has been set, the application will redirect to the starting url specified in the state parameter.

LOGIN URL:
https://account-pre.fifa.com/ab180054-8ef1-4762-9cde-824188b5cf03/oauth2/v2.0/authorize?p=b2c_1a_fifa_signuporsignin&client_Id=b36f4b20-e1bc-46d8-ae62-d1f12ee00c72&nonce=defaultNonce&redirect_uri=http%3A%2F%2Flocalhost&scope=openid%20offline_access&response_type=code&response_mode=form_post&state=aHR0cDovL3d3dy5xYS5maWZhLmNvbS8jbG9naW4=&ui_locales=en