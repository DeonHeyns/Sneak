using ServiceStack;
using Sneaky;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sneak
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("Starting to call!");

			var jc = new JsonServiceClient("http://localhost:53471");
			Parallel.For(0, 1000, i =>
			{

				var person = new AddPerson { FirstName = "FirstName " + i, LastName = "LastName " + i };
				var cars = new List<Car>();
				for (int j = 0; j < 3; j++)
				{
					cars.Add(new Car { Make = "BMW" + j });
				}

				person.Cars.AddRange(cars);

				Console.Clear();
				Console.Write("Calling...");
				jc.Post(person);
			});
			Console.Write("Done...");
			Console.ReadKey();
		}
	}
}
