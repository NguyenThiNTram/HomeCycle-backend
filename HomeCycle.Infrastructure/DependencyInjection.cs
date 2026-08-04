using FluentValidation;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Application.Interfaces.Services.Agreements;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.Profiles;
using HomeCycle.Application.Mappings;
using HomeCycle.Application.Services.Agreements;
using HomeCycle.Application.Services.Auths;
using HomeCycle.Application.Services.Moderators;
using HomeCycle.Application.Services.Payments;
using HomeCycle.Application.Services.Profiles;
using HomeCycle.Application.Validations.Auths;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Externals;
using HomeCycle.Infrastructure.Repositories.Agreements;
using HomeCycle.Infrastructure.Repositories.Appointments;
using HomeCycle.Infrastructure.Repositories.Banks;
using HomeCycle.Infrastructure.Repositories.Orders;
using HomeCycle.Infrastructure.Repositories.Payments;
using HomeCycle.Infrastructure.Repositories.Profiles;
using HomeCycle.Infrastructure.Repositories.Shipments;
using HomeCycle.Infrastructure.Repositories.Users;
using HomeCycle.Infrastructure.Repositories.Wallets;
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

            // register Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonalProfileRepository, PersonalProfileRepository>();
            services.AddScoped<IOtpRepository, OtpRepository>();
            services.AddScoped<IBankAccountRepository, BankAccountRepository>();
            services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();
            services.AddScoped<IBusinessDocumentRepository, BusinessDocumentRepository>();
            services.AddScoped<IBusinessProductTypeRepository, BusinessProductTypeRepository>();
            services.AddScoped<IBusinessServiceAreaRepository, BusinessServiceAreaRepository>();
            services.AddScoped<IBusinessProcurementPreferenceRepository, BusinessProcurementPreferenceRepository>();
            services.AddScoped<IAgreementFormRepository, AgreementFormRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddScoped<IWalletLedgerRepository, WalletLedgerRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<ICollectionAppointmentRepository, CollectionAppointmentRepository>();
            services.AddScoped<IInspectionAppointmentRepository, InspectionAppointmentRepository>();
            services.AddScoped<IShipmentRepository, ShipmentRepository>();

            // register Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBusinessProfileService, BusinessProfileService>();
            services.AddScoped<IModeratorService, ModeratorService>();
            //services.AddScoped<IAgreementFormService, AgreementFormService>();
            services.AddScoped<IPaymentService, PaymentService>();



            services.Configure<PayOSSettings>(configuration.GetSection("PayOS"));
            services.AddScoped<IPaymentGatewayService, PayOSGatewayAdapter>();

            return services;
        }
    }
}
