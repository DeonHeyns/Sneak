using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace Sneaky
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration) => Configuration = configuration;

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseServiceStack(new AppHost
			{
				AppSettings = new NetCoreAppSettings(Configuration)
			});

			app.Run(async (context) =>
			{
				context.Response.Redirect("/metadata");
				await Task.FromResult(1);
			});
		}
	}

	public class AppHost : AppHostBase
	{
		public AppHost()
			: base("Hello Services", typeof(HelloServices).Assembly)
		{

		}

		public override void Configure(Container container)
		{
			container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory("Server=.;Database=Junkyard;User Id=sa;Password=Yours;", SqlServer2016Dialect.Provider));
			var a = new AutoQueryFeature();
			Plugins.Add(a);
			Plugins.Add(new PostmanFeature());

			var dbFactory = container.Resolve<IDbConnectionFactory>();

			using (var db = dbFactory.OpenDbConnection())
			{
				var created = db.CreateTableIfNotExists<Person>();
				db.CreateTableIfNotExists<Car>();

				if (created)
				{
					for (int i = 0; i < 300; i++)
					{
						var person = new Person { FirstName = "First " + i, LastName = "Last " + i };
						var cars = new List<Car>();
						for (int j = 0; j < 3; j++)
						{
							cars.Add(new Car { Make = "BMW" + j });
						}

						person.Cars.AddRange(cars);
						db.Save(person, references: true);
					}
				}
			}
		}
	}

	public class HelloServices : Service
	{
		public object Any(AddPerson request)
		{
			var person = request.ConvertTo<Person>();
			var exCount = 0;
			try
			{
				using (var tran = Db.OpenTransaction())
				{
					Db.Save(person, references: true);

					for (int i = 0; i < 10; i++)
					{
						Gateway.Send(new People { Id = person.Id });
						Db.LoadSingleById<Person>(person.Id);
					}
				}
			}
			catch (Exception ex)
			{
				var a = ex;
				exCount++;
			}

			return person;
		}
	}

	[Route("/person")]
	public class AddPerson : IReturn<Person[]>
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public List<Car> Cars { get; set; } = new List<Car>();
	}

	[Route("/people")]
	public class People : QueryDb<Person> {
		public long? Id { get; set; }
	}
	public class Person
	{
		[AutoIncrement, PrimaryKey, Index]
		public long Id { get; set; }

		public string FirstName { get; set; }
		public string LastName { get; set; }
		[Reference]
		public List<Car> Cars { get; set; } = new List<Car>();
	}

	public class Car
	{
		[AutoIncrement, PrimaryKey, Index]
		public long Id { get; set; }

		public long PersonId { get; set; }

		public string Make { get; set; }
	}
}
