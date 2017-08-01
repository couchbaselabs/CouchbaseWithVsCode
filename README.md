# Using Couchbase .NET Core SDK without Windows #
As of release 2.4.0, the Couchbase .NET SDK provides support for .NET Core 1.0. .NET Core is a new framework that allows .NET to run on OSX, Linux and Windows. For developing on Windows, Visual Studio is the standard for .NET Core development using Couchbase Server. For OSX and Linux, there are other development alternatives:

- Visual Studio for Mac
- VSCode
- Jetbrains Rider IDE
- and others...

## Developing with VSCode ##

### Prerequisites ###
To develop with VSCode, you'll need to install the following:

- Install [VSCode](http://code.visualstudio.com/Docs/setup "VSCode")
- Install [.NET Core](http://code.visualstudio.com/Docs/setup ".NET Core")

You will also need to having an instance [Couchbase server](https://www.couchbase.com/downloads) running locally, a [VM cluster](https://github.com/couchbaselabs/vagrants), or a remote cluster that you have access to. 

### Creating the Project ###
Start by opening VSCode and using the CLI tool to generate the ASP.NET project using scaffolding. In the VSCode terminal (Ctrl+`), type the following:

    mkdir CouchbaseWithVsCode
	cd CouchbaseWithVsCode
	dotnet new mvc

This will create the directory structure and a MVC Web application project for our application. Using VSCode, open the folder (CouchbaseWithVsCode) created in the previous step. To do this select `File` and then `Open Folder` and navigate to the CouchbaseWithCode folder. Once you have done this, the directory structure will load into the Explorer pane of VSCode. You should see the following listing:

[Image of dir]

Now we will add a dependency to the offical Couchbase .NET SDK using NuGet. In the VSCode terminal, type the following and press enter:

	dotnet add package CouchbaseNetClient
	dotnet restore

This will install the Couchbase .NET SDK and all of it's dependencies. Additionally, it will restore any other dependencies that the project depends on. At this point the project should be functional and you can test it by running the following command from  the terminal:
	
	dotnet run

Open a browser and type in `http://localhost:5000` (or whatever port the terminal tells you it is listening on) and you should see the `CouchbaseWithVSCode` Web application.

[insert screenshot of app]


Now that we have the ASP.NET Core Web application working, its time to add some code to connect to Couchbase server! 

### Bootstrapping the SDK ###

There is a basic pattern for using the client within your applications that involves creating and initializing the `Cluster` and `IBucket` objects when the application starts up, and disposing/closing them when the application shuts down. In a .NET Core ASP.NET application this means using the `Startup.cs` class as a base for our bootstrapping code and tear down code by configuring methods with cancellation tokens for `ApplicationStarted` and `ApplicationStopped` via `IApplicationLifetime`. 

To do this, open up the `Setup.cs` file in your `VSCode` editor and add the following usage statements to the top of the file, below the other using statements:

	using Couchbase;
	using Couchbase.Configuration.Client;

In the `Startup` class, locate the `Configure(...)` method and add a new parameter for `IApplicationLifetime` and register two methods that will provide cancellation callbacks for `ApplicationStopping` and `ApplicationStarting`:

	public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
	{
		...
		
		appLifetime.ApplicationStarting.Register(OnStartup);
		appLifetime.ApplicationStopping.Register(OnShutdown);
	}

Add the methods `OnStartup` and `OnShutdown` to the `Startup` class:

	private void OnStartup()
	{
		var config = new ClientConfiguration
        {
            Servers = new List<Uri>
            {
                new Uri("http://localhost:8091/")
            }
        };
		
		ClusterHelper.Initialize(config);
	} 

	private void OnShutdown()
	{
		ClusterHelper.Close();
	}

This will create a default configuration that uses localhost as the bootstrapping server, if you are using a VM or a remote cluster, use the IP or hostname for that cluster. We then call `ClusterHelper.Initialize(config)` to ensure that the configuration has been parsed and the client is ready to open a bucket. Note that we could have used the Cluster object directly, but that would add slightly more complexity when we go to retrieve a bucket reference per request. Additionally, the `ClusterHelper` will cache any bucket objects that it opens making it easier to manage resources. The full code snippet can be found here.

Since we are using ASP.NET Core DI (Dependency Injection), we'll register an `IBucket` instance as a singleton in the `ConfigureServices` method:

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Add framework services.
        services.AddMvc();

        // create a func for creating the bucket instance after app has initialized
        IBucket Provider(IServiceProvider serviceProvider) => ClusterHelper.GetBucket("default");

        // add the func as singleton service - once per application
        services.AddSingleton(Provider);
    }

Now, in our controllers, we'll just have to provide constructors that take an `IBucket` implementation reference to use the Couchbase SDK in your application.

### Using the Bucket object in your Controller ###
Now that the application has been configured for Couchbase server, it's time to use the client in your application! To do this, navigate to the `Controllers` directory in the `VSCode` `Explorer` and open the `HomeController.cs` file (this was created as scaffolding when you created the project using the CIL) and modify the class by adding a constructor overload which takes a `IBucket` implementation:

	using Microsoft.AspNetCore.Mvc
	using Couchbase;
	using Couchbase.Core;

	namespace CouchbaseWithVSCode.Controllers
	{
		public class HomeController : Controller
		{
			IBucket _bucket;

			public HomeController(IBucket bucket)
			{
				_bucket = bucket;
			}
	
			...
		}	
	}

This will inject the `IBucket` reference into your Controller class, caching it in the process, so that you can to insert and fetch data from Couchbase server. To do this, add the following code to the `Index` method:	


    public IActionResult Index()
    {
        _bucket.Upsert("thekey", new { welcomeMsg = "Welcome to my app."});
        return View();
    }

Here we are just inserting a document with a key of `thekey` with a simple welcome message.
 
In the `About` action method, we'll perform a read operation using `Get` to retrieve the key and the message: 

    public IActionResult About()
    {
        ViewData["Message"] = _bucket.Get<dynamic>("thekey").Value.welcomeMsg;

        return View();
    }

Note that this is a simplified, contrived example just to illustrate how to setup the application! Your actual application will likely be much more complex and use additional Couchbase features such as N1QL or perhaps the Sub-Document API to increase performance!

Once you have done this, we will first build the application (after saving our changes) and then run it using the following commands in the Integrated terminal:

	dotnet build
    dotnet run

If this is successful, you'll get output similar to the following:
	
	PS C:\CouchbaseWithVsCode> dotnet run
	Hosting environment: Production
	Content root path: C:\CouchbaseWithVsCode
	Now listening on: http://localhost:5000
	Application started. Press Ctrl+C to shut down.

If you navigate to the http://localhost:5000 you should see the app working. The cool thing here is that you should be able to do the steps above on MacOS, Linux or Windows without anything changing!





