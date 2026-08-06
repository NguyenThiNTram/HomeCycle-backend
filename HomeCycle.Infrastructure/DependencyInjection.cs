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
using HomeCycle.Application.Interfaces.Repositories.Media;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Application.Interfaces.Services.Agreements;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.Profiles;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Application.Interfaces.Services.Offers;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Application.Interfaces.Services.Users;
using HomeCycle.Application.Mappings;
using HomeCycle.Application.Services.Agreements;
using HomeCycle.Application.Services.Auths;
using HomeCycle.Application.Services.Personals;
using HomeCycle.Application.Services.Offers;
using HomeCycle.Application.Services.Posts;
using HomeCycle.Application.Services.Products;
using HomeCycle.Application.Services.Moderators;
using HomeCycle.Application.Services.Payments;
using HomeCycle.Application.Services.Profiles;
using HomeCycle.Application.Validations.Auths;
using HomeCycle.Application.Validations.Users;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Externals;
using HomeCycle.Infrastructure.Repositories.Agreements;
using HomeCycle.Infrastructure.Repositories.Appointments;
using HomeCycle.Infrastructure.Repositories.Banks;
using HomeCycle.Infrastructure.Repositories.Orders;
using HomeCycle.Infrastructure.Repositories.Payments;
using HomeCycle.Infrastructure.Repositories.Profiles;
using HomeCycle.Infrastructure.Repositories.Offers;
using HomeCycle.Infrastructure.Repositories.Posts;
using HomeCycle.Infrastructure.Repositories.Products;
using HomeCycle.Infrastructure.Repositories.Shipments;
using HomeCycle.Infrastructure.Repositories.Users;
using HomeCycle.Infrastructure.Repositories.Wallets;
using HomeCycle.Infrastructure.Security;
using HomeCycle.Infrastructure.UnitOfWorks;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HomeCycle.Application.Validations.Agreements;
using HomeCycle.Application.DTOs.Requests.Agreements;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
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
            // do nằm chung 1 application nên chỉ cần gọi 1 lần là đủ, không cần gọi nhiều lần
            services.AddValidatorsFromAssemblyContaining<RegisterPersonalRequestValidator>();
            services.AddValidatorsFromAssembly(typeof(LoginRequestValidator).Assembly);
            services.RemoveAll(typeof(IValidator<AgreementDetailsDto>));
            services.RemoveAll(typeof(AgreementDetailsDtoValidator));

            // register External Services
            services.AddScoped<IFileStorageService, FirebaseStorageService>();
            services.AddScoped<IOtpRepository, OtpRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBrandRepository, BrandRepository>();

            // register Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPersonalProfileRepository, PersonalProfileRepository>();
            services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IBankAccountRepository, BankAccountRepository>();
            services.AddScoped<IProductAttributeOptionRepository, ProductAttributeOptionRepository>();
            services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();
            services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IMediaRepository, MediaRepository>();
            services.AddScoped<IProductAttributeValueRepository, ProductAttributeValueRepository>();
            services.AddScoped<IOfferRepository, OfferRepository>();
            services.AddScoped<INegotiationRepository, NegotiationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
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
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPersonalProfileService, PersonalProfileService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IProductTypeService, ProductTypeService>();
            services.AddScoped<IProductAttributeService, ProductAttributeService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddScoped<IProductAttributeOptionService, ProductAttributeOptionService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IBusinessProfileService, BusinessProfileService>();
            services.AddScoped<IModeratorService, ModeratorService>();
            services.AddScoped<IAgreementFormService, AgreementFormService>();
            services.AddScoped<IPaymentService, PaymentService>();



            services.Configure<PayOSSettings>(configuration.GetSection("PayOS"));
            services.AddScoped<IPaymentGatewayService, PayOSGatewayAdapter>();

            return services;
        }
    }
}
