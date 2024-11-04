using CommentApp.Common.Data;
using CommentApp.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomLogging(builder.Configuration);

builder.Services.ConfigureDatabase(builder.Configuration);

builder.Services.AddRepositories();
builder.Services.AddServices();

builder.Services.AddRedis(builder.Configuration);

builder.Services.AddKafka(builder.Configuration);

builder.Services.AddCustomHttpClient(builder.Configuration);

builder.Services.AddBackgroundServiceOptions(builder.Configuration);

builder.Services.AddBackgroundServices();

builder.Services.ConfigureKestrelServer(builder.Configuration, builder.WebHost);

await builder.Services.AddAmazonS3(builder.Configuration);

builder.Services.AddAutoMapper(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CommentsAppDbContext>();
    dbContext.Database.Migrate();
}

app.MapControllers();

app.Run();
