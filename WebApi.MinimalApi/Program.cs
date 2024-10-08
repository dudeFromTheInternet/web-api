using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });

builder.Services.AddControllers(options =>
{
    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
    options.ReturnHttpNotAcceptable = true;
    options.RespectBrowserAcceptHeader = true;
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(userEntity => $"{userEntity.LastName} {userEntity.FirstName}"));
    cfg.CreateMap<PostUserDto, UserEntity>();
    cfg.CreateMap<UpdateUserDto, UserEntity>();
}, new System.Reflection.Assembly[0]);

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

app.MapControllers();

app.Run();