using ApiServer;
using System.Reflection;

ApiServerBuilder.Run(args,
    Assembly.Load("CredentialService"),
    Assembly.Load("ServiceA"));
