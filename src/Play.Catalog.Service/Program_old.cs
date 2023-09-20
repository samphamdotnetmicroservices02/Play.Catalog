// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Play.Catalog.Service;
// using Play.Catalog.Service.Entities;
// using Play.Common.HealthChecks;
// using Play.Common.Identity;
// using Play.Common.MassTransit;
// using Play.Common.MongoDB;
// using Play.Common.Settings;

// var builder = WebApplication.CreateBuilder(args);

// ServiceSettings serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
// const string AllowedOriginSetting = "AllowedOrigin";

// builder.Services
//     .AddMongo()
//     .AddMongoRepository<Item>("items")
//     .AddMassTransitWithMessageBroker(builder.Configuration)
//     .AddJwtBearerAuthentication();

// builder.Services.AddAuthorization(options =>
// {
//     options.AddPolicy(Policies.Read, policy =>
//     {
//         policy.RequireRole("Admin");
//         policy.RequireClaim("scope", "catalog.readaccess", "catalog.fullaccess");
//     });

//     options.AddPolicy(Policies.Write, policy =>
//     {
//         policy.RequireRole("Admin");
//         policy.RequireClaim("scope", "catalog.writeaccess", "catalog.fullaccess");
//     });
// });

// builder.Services.AddControllers(options =>
// {
//     options.SuppressAsyncSuffixInActionNames = false;
// });
// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
// builder.Services.AddHealthChecks()
//     .AddMongoDb();


// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
//     app.UseCors(builder =>
//     {
//         builder.WithOrigins(app.Configuration[AllowedOriginSetting])
//             .AllowAnyHeader() //Allows any the headers that the client want to send
//             .AllowAnyMethod(); //Allows any the methods the client side want to use including GET, POST, PUT and all other verbs
//     });
// }

// app.UseHttpsRedirection();

// app.UseAuthentication();

// app.UseAuthorization();

// app.MapControllers();
// app.MapPlayEconomyHealthCheck();

// app.Run();
