using FluentValidation;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Application.Interfaces.Services.Users;
using HomeCycle.Application.Mappings;
using HomeCycle.Application.Services.Auths;
using HomeCycle.Application.Services.Personals;
using HomeCycle.Application.Validations.Auths;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Externals;
using HomeCycle.Infrastructure.Repositories.Banks;
using HomeCycle.Infrastructure.Repositories.Users;
using HomeCycle.Infrastructure.Security;
using HomeCycle.Infrastructure.UnitOfWorks;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            //register DB
            services.AddDbContext<HomeCycleDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            });

            //register UOW
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //register hash password
            services.AddScoped<
                Microsoft.AspNetCore.Identity.IPasswordHasher<object>,
                Microsoft.AspNetCore.Identity.PasswordHasher<object>>();

            services.AddScoped<IPasswordHasher, PasswordHasherService>();

            //register JWT
            services.AddScoped<IJwtService, JwtService>();

            //register AutoMapper
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

            // register FluentValidation
            services.AddValidatorsFromAssemblyContaining<RegisterPersonalRequestValidator>();

            // register External Services
            services.AddScoped<IFileStorageService, FirebaseStorageService>();
            services.AddScoped<IOtpRepository, OtpRepository>();
            services.AddScoped<IEmailService, EmailService>();

            // register Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonalProfileRepository, PersonalProfileRepository>();
            
            services.AddScoped<IBankAccountRepository, BankAccountRepository>();

            // register Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPersonalProfileService, PersonalProfileService>();
            
            return services;
        }
    }
}
