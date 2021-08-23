'''
sudo docker pull mcr.microsoft.com/mssql/server:2017-latest
'''

'''
sudo docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=fickdich" \
   -p 1433:1433 --name mssql -h mssql \
   -d \
   mcr.microsoft.com/mssql/server:2017-latest
'''

'''
dotnet fake build -t RunDbMigrations
'''
oder 
'''
dotnet run 'Server=127.0.0.1,1401; Database=Master; User Id=SA; Password=dickdich' '../../database'
''''


Yobo.Server/local.settings.json:
'''
{
    "MailChimpApiKey": "ABCD",
    "ReadDbConnectionString": "Server=127.0.0.1,1401; Database=Master; User Id=SA; Password=fickdich",

    "AuthIssuer": "authIssuer",
    "AuthAudience": "http://localhost:8080",
    "AuthSecret": "http://localhost:8080",
    "AuthTokenLifetime":"60",
    "EmailsFromName":"name@domain.com",
    "MailjetApiKey": "mailjet",
    "MailjetSecretKey": "secretKey",

    "ServerBaseUrl": "http://localshost:1401",
    "AdminEmail": "admin@domain.com",
    "AdminPassword": "secret",

    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=True",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet"
    }
}
'''