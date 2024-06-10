using SignalRService.Hubs;
using SignalRService.Request;
using SignalRService.Service;
using SignalRService.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IChatServiceValidation, ChatServiceValidation>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<IChatHttpRequest, ChatHttpRequest>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.UseMiddleware<FirstMiddleware>();
app.MapHub<ChatHub>("/chatHub/{clientId}");

// app.MapHub<ChatHub>("/chatHub");

// app.Map("/chatHub/{app}", app => {
//    app.UseMiddleware<FirstMiddleware>();
//    app.UseEndpoints(endpoints => {
//     endpoints.MapHub<ChatHub>("/chatHub");
//    });
// });

app.UseStatusCodePagesWithRedirects("/Index?status={0}");

app.Run();
