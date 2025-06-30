using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using api_management.Data;
using api_management.helper;
using api_management;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(option =>
{

    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
// dang ky identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // options.Password.RequireDigit = true; // Yêu cầu mật khẩu phải có ít nhất 1 chữ số
    // options.Password.RequireLowercase = true; // Yêu cầu mật khẩu phải có ít nhất 1 chữ thường
    // options.Password.RequireUppercase = true; // Yêu cầu mật khẩu phải có ít nhất 1 chữ hoa
    // options.Password.RequireNonAlphanumeric = true; // Yêu cầu mật khẩu phải có ít nhất 1 ký tự đặc biệt
    // options.Password.RequiredLength = 6; // Độ dài tối thiểu của mật khẩu

    options.User.RequireUniqueEmail = true; // Yêu cầu email phải là duy nhất
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ "; // Ký tự hợp lệ cho tên người dùng
    options.SignIn.RequireConfirmedEmail = true;// Cấu hình xác thực địa chỉ email (email phải tồn tại)


});
// dang ky jwt bear
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new
    Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, 
        ClockSkew = TimeSpan.Zero, 
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
    
});     
builder.Services.AddSingleton<EmailSender>();

//them authen cho swagger
builder.Services.AddSwaggerGen(options =>
{
    // Thêm mô tả chuẩn Bearer JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token theo dạng: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseStaticFiles(); 
app.UseAuthorization();
app.MapControllers();

app.Run();