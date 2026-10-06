using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var seedSettings = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            logger.LogInformation("Starting database idempotent seed...");

            // 0. Ensure schema columns exist
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Donations' AND COLUMN_NAME = 'DonorCity') " +
                    "ALTER TABLE Donations ADD DonorCity NVARCHAR(100) NULL; " +
                    "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DonationReceipts' AND COLUMN_NAME = 'DigitalSignature') " +
                    "ALTER TABLE DonationReceipts ADD DigitalSignature NVARCHAR(200) NULL;");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema migration check completed or skipped.");
            }

            // 1. Seed Roles
            string[] roles = { "SuperAdmin", "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Seeded role: {Role}", role);
                }
            }

            // 2. Seed SuperAdmin
            var superAdmin = await userManager.FindByEmailAsync(seedSettings.SuperAdminEmail);
            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = seedSettings.SuperAdminEmail,
                    Email = seedSettings.SuperAdminEmail,
                    FullName = seedSettings.SuperAdminFullName,
                    PhoneNumber = "+923001234567",
                    EmailConfirmed = true,
                    Address = "Give-AID Central HQ, Blue Area",
                    City = "Islamabad",
                    Country = "Pakistan",
                    Profession = "Chief Executive Director",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superAdmin, seedSettings.SuperAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRolesAsync(superAdmin, new[] { "SuperAdmin", "Admin" });
                    logger.LogInformation("Seeded SuperAdmin account: {Email}", seedSettings.SuperAdminEmail);
                }
            }
            else
            {
                superAdmin.IsActive = true;
                superAdmin.EmailConfirmed = true;
                await userManager.UpdateAsync(superAdmin);

                if (!await userManager.CheckPasswordAsync(superAdmin, seedSettings.SuperAdminPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(superAdmin);
                    await userManager.ResetPasswordAsync(superAdmin, token, seedSettings.SuperAdminPassword);
                }

                if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                if (!await userManager.IsInRoleAsync(superAdmin, "Admin"))
                    await userManager.AddToRoleAsync(superAdmin, "Admin");
            }

            // 3. Seed Admin
            var admin = await userManager.FindByEmailAsync(seedSettings.AdminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = seedSettings.AdminEmail,
                    Email = seedSettings.AdminEmail,
                    FullName = seedSettings.AdminFullName,
                    PhoneNumber = "+923007654321",
                    EmailConfirmed = true,
                    Address = "Welfare Operations Center, Gulberg III",
                    City = "Lahore",
                    Country = "Pakistan",
                    Profession = "Operations Director",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, seedSettings.AdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Seeded Admin account: {Email}", seedSettings.AdminEmail);
                }
            }
            else
            {
                admin.IsActive = true;
                admin.EmailConfirmed = true;
                await userManager.UpdateAsync(admin);

                if (!await userManager.CheckPasswordAsync(admin, seedSettings.AdminPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                    await userManager.ResetPasswordAsync(admin, token, seedSettings.AdminPassword);
                }

                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // 4. Seed Demo Donor User
            var demoUser = await userManager.FindByEmailAsync(seedSettings.DemoUserEmail);
            if (demoUser == null)
            {
                demoUser = new ApplicationUser
                {
                    UserName = seedSettings.DemoUserEmail,
                    Email = seedSettings.DemoUserEmail,
                    FullName = seedSettings.DemoUserFullName,
                    PhoneNumber = "+923219876543",
                    EmailConfirmed = true,
                    Address = "Sector F-10/2",
                    City = "Islamabad",
                    Country = "Pakistan",
                    Profession = "Senior Software Engineer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(demoUser, seedSettings.DemoUserPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(demoUser, "User");
                    logger.LogInformation("Seeded Demo User account: {Email}", seedSettings.DemoUserEmail);
                }
            }
            else
            {
                demoUser.IsActive = true;
                demoUser.EmailConfirmed = true;
                await userManager.UpdateAsync(demoUser);

                if (!await userManager.CheckPasswordAsync(demoUser, seedSettings.DemoUserPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(demoUser);
                    await userManager.ResetPasswordAsync(demoUser, token, seedSettings.DemoUserPassword);
                }

                if (!await userManager.IsInRoleAsync(demoUser, "User"))
                    await userManager.AddToRoleAsync(demoUser, "User");
            }

            // 5. Seed Donation Causes
            if (!await context.DonationCauses.AnyAsync())
            {
                var causes = new List<DonationCause>
                {
                    new()
                    {
                        Title = "Children Welfare & Protection",
                        Code = "CHILD-WELFARE",
                        Category = "Children",
                        Description = "Providing nutrition, essential healthcare, clean shelter, and psychological rehabilitation for underprivileged and orphaned children across rural and suburban regions.",
                        TargetAmount = 2500000m,
                        RaisedAmount = 1450000m,
                        ImageUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Education for Every Child",
                        Code = "EDU-FOR-ALL",
                        Category = "Education",
                        Description = "Funding school tuition fees, modern textbooks, STEM learning kits, and school uniform kits for bright students from low-income households.",
                        TargetAmount = 3000000m,
                        RaisedAmount = 1890000m,
                        ImageUrl = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Disabled Persons Empowerment",
                        Code = "DISABLE-CARE",
                        Category = "Disabled Persons",
                        Description = "Supplying high-quality wheelchairs, prosthetic limbs, hearing aids, and accessible vocational technology for differently-abled individuals.",
                        TargetAmount = 1800000m,
                        RaisedAmount = 920000m,
                        ImageUrl = "https://images.unsplash.com/photo-1516307365426-bea591f05011?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Women Welfare & Micro-Enterprise",
                        Code = "WOMEN-EMPOWER",
                        Category = "Women",
                        Description = "Equipping widows and aspiring female entrepreneurs with sewing machines, digital literacy workshops, micro-grants, and business mentorship.",
                        TargetAmount = 2000000m,
                        RaisedAmount = 1120000m,
                        ImageUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Youth Welfare & Skill Development",
                        Code = "YOUTH-SKILLS",
                        Category = "Youth",
                        Description = "Empowering young men and women with IT certifications, software development bootcamps, and career counseling to bridge the employment gap.",
                        TargetAmount = 1500000m,
                        RaisedAmount = 640000m,
                        ImageUrl = "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 5,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Elderly Welfare & Healthcare",
                        Code = "ELDERLY-CARE",
                        Category = "Elderly",
                        Description = "Monthly geriatric healthcare packs, specialized cardiac and diabetic medicines, and compassionate hospice care for senior citizens without family support.",
                        TargetAmount = 1200000m,
                        RaisedAmount = 780000m,
                        ImageUrl = "https://images.unsplash.com/photo-1581579438747-1dc8d17bbce4?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 6,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Emergency Relief & Flood Aid",
                        Code = "EMERGENCY-RELIEF",
                        Category = "Emergency",
                        Description = "Rapid response distribution of dry food rations, clean drinking water tanks, waterproof family tents, and emergency survival kits in disaster zones.",
                        TargetAmount = 5000000m,
                        RaisedAmount = 3450000m,
                        ImageUrl = "https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = true,
                        DisplayOrder = 7,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Free Medical & Eye Surgical Camps",
                        Code = "HEALTH-CARE",
                        Category = "Healthcare",
                        Description = "Conducting mobile outpatient clinics, diagnostic laboratory screenings, and free cataract replacement surgeries for patients in remote areas.",
                        TargetAmount = 2200000m,
                        RaisedAmount = 1350000m,
                        ImageUrl = "https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80",
                        IsActive = true,
                        IsFeatured = false,
                        DisplayOrder = 8,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.DonationCauses.AddRange(causes);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} donation causes.", causes.Count);
            }

            // 6. Seed NGOs
            if (!await context.NGOs.AnyAsync())
            {
                var ngos = new List<NGO>
                {
                    new()
                    {
                        Name = "Hope for Tomorrow Foundation",
                        RegistrationNumber = "NGO-ISB-2018-091",
                        Description = "A pioneer non-governmental organization dedicated to breaking cycles of poverty through primary education, clean water, and child rights advocacy.",
                        LogoUrl = "https://images.unsplash.com/photo-1599305445671-ac291c95aaa9?w=300&auto=format&fit=crop&q=80",
                        Email = "info@hopefortomorrow.org",
                        Phone = "+92-51-2859011",
                        Website = "https://hopefortomorrow.org",
                        Address = "Plot 44, Industrial Area, I-9/3",
                        City = "Islamabad",
                        Country = "Pakistan",
                        EstablishedYear = 2014,
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Al-Khidmat Humanitarian Network",
                        RegistrationNumber = "NGO-LHR-2012-442",
                        Description = "Extensive nationwide disaster management, healthcare centers, orphan family sponsorships, and clean drinking water filtration plants.",
                        LogoUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=300&auto=format&fit=crop&q=80",
                        Email = "contact@alkhidmathumans.org",
                        Phone = "+92-42-3588900",
                        Website = "https://alkhidmathumans.org",
                        Address = "Main Boulevard, Model Town",
                        City = "Lahore",
                        Country = "Pakistan",
                        EstablishedYear = 2008,
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Bright Horizon Education Trust",
                        RegistrationNumber = "NGO-KHI-2016-881",
                        Description = "Operating community schools and technical training centers in marginalized urban settlements to foster literacy and digital inclusion.",
                        LogoUrl = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=300&auto=format&fit=crop&q=80",
                        Email = "support@brighthorizon.org",
                        Phone = "+92-21-34981122",
                        Website = "https://brighthorizon.org",
                        Address = "Clifton Block 5",
                        City = "Karachi",
                        Country = "Pakistan",
                        EstablishedYear = 2016,
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Care & Cure Medical Relief",
                        RegistrationNumber = "NGO-RWP-2019-331",
                        Description = "Providing free dialysis treatments, mother-and-child healthcare units, and subsidized life-saving pharmaceuticals.",
                        LogoUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&auto=format&fit=crop&q=80",
                        Email = "care@curemedical.org",
                        Phone = "+92-51-4412390",
                        Website = "https://curemedical.org",
                        Address = "Peshawar Road, Saddar",
                        City = "Rawalpindi",
                        Country = "Pakistan",
                        EstablishedYear = 2019,
                        IsActive = true,
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.NGOs.AddRange(ngos);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} NGOs.", ngos.Count);
            }

            // 7. Seed Partners
            if (!await context.Partners.AnyAsync())
            {
                var partners = new List<Partner>
                {
                    new()
                    {
                        Name = "Global Tech Philanthropy",
                        Description = "Corporate CSR partner funding software training bootcamps and digital infrastructure for welfare schools.",
                        LogoUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=300&auto=format&fit=crop&q=80",
                        Website = "https://globaltechphilanthropy.com",
                        ContactEmail = "csr@globaltech.com",
                        ContactPhone = "+1-800-555-0199",
                        DisplayOrder = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "United Humanity Alliance",
                        Description = "International humanitarian alliance providing emergency relief logistics, water purification units, and medical tents.",
                        LogoUrl = "https://images.unsplash.com/photo-1560179707-f14e90ef3623?w=300&auto=format&fit=crop&q=80",
                        Website = "https://unitedhumanityalliance.org",
                        ContactEmail = "partner@unitedhumanity.org",
                        ContactPhone = "+44-20-7946-0912",
                        DisplayOrder = 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Apex Microfinance Group",
                        Description = "Collaborating to issue zero-interest micro-finance loans and equipment to female entrepreneurs.",
                        LogoUrl = "https://images.unsplash.com/photo-1554774853-719586f82d77?w=300&auto=format&fit=crop&q=80",
                        Website = "https://apexfinance.org",
                        ContactEmail = "welfare@apexfinance.org",
                        ContactPhone = "+92-42-111-222-333",
                        DisplayOrder = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "National Relief & Rescue Consortium",
                        Description = "Disaster management network assisting in supply chain transportation and rapid search-and-rescue.",
                        LogoUrl = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=300&auto=format&fit=crop&q=80",
                        Website = "https://nationalreliefconsortium.pk",
                        ContactEmail = "info@nationalrelief.pk",
                        ContactPhone = "+92-51-111-999-000",
                        DisplayOrder = 4,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Partners.AddRange(partners);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} Partners.", partners.Count);
            }

            // 8. Seed Programmes
            if (!await context.Programmes.AnyAsync())
            {
                var ngo = await context.NGOs.FirstOrDefaultAsync();
                var programmes = new List<Programme>
                {
                    new()
                    {
                        Title = "Winter Warmth & Blanket Distribution 2026",
                        Category = "Emergency",
                        Description = "Distributing high-grade thermal blankets, woolen sweaters, waterproof tarpaulins, and heater supplies to mountain communities and shelter homes.",
                        Location = "Northern Areas & Swat Valley",
                        StartDate = DateTime.UtcNow.AddDays(7),
                        EndDate = DateTime.UtcNow.AddDays(21),
                        ImageUrl = "https://images.unsplash.com/photo-1517486808906-6ca8b3f04846?w=800&auto=format&fit=crop&q=80",
                        Capacity = 500,
                        Status = ProgrammeStatus.Upcoming,
                        IsFeatured = true,
                        NGOId = ngo?.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Free Cataract Eye Surgery & Medical Camp",
                        Category = "Healthcare",
                        Description = "A 3-day comprehensive medical screening and laser cataract surgical camp with specialized ophthalmologists and free post-op prescription lenses.",
                        Location = "District Hospital, Rahim Yar Khan",
                        StartDate = DateTime.UtcNow.AddDays(14),
                        EndDate = DateTime.UtcNow.AddDays(17),
                        ImageUrl = "https://images.unsplash.com/photo-1579684385127-1ef15d508118?w=800&auto=format&fit=crop&q=80",
                        Capacity = 1000,
                        Status = ProgrammeStatus.Upcoming,
                        IsFeatured = true,
                        NGOId = ngo?.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Youth Digital Skills & Freelance Bootcamp",
                        Category = "Youth",
                        Description = "Intensive 8-week bootcamp teaching full-stack web development, graphic design, and freelance marketplace bidding strategies to underprivileged college graduates.",
                        Location = "Give-AID Innovation Hub, Islamabad",
                        StartDate = DateTime.UtcNow.AddDays(30),
                        EndDate = DateTime.UtcNow.AddDays(86),
                        ImageUrl = "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=800&auto=format&fit=crop&q=80",
                        Capacity = 150,
                        Status = ProgrammeStatus.Upcoming,
                        IsFeatured = true,
                        NGOId = ngo?.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Clean Drinking Water Filtration Plant Installation",
                        Category = "General",
                        Description = "Installing 5 solar-powered reverse osmosis clean water plants delivering potable drinking water to over 15,000 residents in arid districts.",
                        Location = "Tharparkar District, Sindh",
                        StartDate = DateTime.UtcNow.AddDays(20),
                        EndDate = DateTime.UtcNow.AddDays(40),
                        ImageUrl = "https://images.unsplash.com/photo-1541888946425-d0fbb18086f6?w=800&auto=format&fit=crop&q=80",
                        Capacity = 300,
                        Status = ProgrammeStatus.Upcoming,
                        IsFeatured = false,
                        NGOId = ngo?.Id,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Programmes.AddRange(programmes);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} Programmes.", programmes.Count);
            }

            // 9. Seed Gallery
            if (!await context.GalleryItems.AnyAsync())
            {
                var programme = await context.Programmes.FirstOrDefaultAsync();
                var gallery = new List<GalleryItem>
                {
                    new()
                    {
                        Title = "Children Receiving Educational Kits",
                        Description = "Smiling students in rural schools receiving new backpacks, textbooks, and art supplies.",
                        ImageUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80",
                        Category = "Education",
                        ProgrammeId = programme?.Id,
                        DisplayOrder = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Wheelchair & Mobility Distribution",
                        Description = "Providing customized wheelchairs to empower individuals with mobility impairments.",
                        ImageUrl = "https://images.unsplash.com/photo-1576765608535-5f04d1e3f289?w=800&auto=format&fit=crop&q=80",
                        Category = "Disabled Persons",
                        ProgrammeId = programme?.Id,
                        DisplayOrder = 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Emergency Food Ration Distribution",
                        Description = "Volunteers loading dry food bags and cooking oil packages for displaced families.",
                        ImageUrl = "https://images.unsplash.com/photo-1593113598332-cd288d649433?w=800&auto=format&fit=crop&q=80",
                        Category = "Emergency",
                        ProgrammeId = programme?.Id,
                        DisplayOrder = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Women Vocational Sewing Center",
                        Description = "Graduation ceremony for women who completed the advanced sewing and garment design workshop.",
                        ImageUrl = "https://images.unsplash.com/photo-1582213782179-e0d53f98f2ca?w=800&auto=format&fit=crop&q=80",
                        Category = "Women",
                        ProgrammeId = programme?.Id,
                        DisplayOrder = 4,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Free Medical OPD & Diagnostics",
                        Description = "Medical team conducting blood tests and health checkups in mobile health clinics.",
                        ImageUrl = "https://images.unsplash.com/photo-1584515933487-779824d29309?w=800&auto=format&fit=crop&q=80",
                        Category = "Healthcare",
                        ProgrammeId = programme?.Id,
                        DisplayOrder = 5,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Title = "Clean Water Plant Inauguration",
                        Description = "Villagers celebrating the first flow of purified drinking water from the newly built RO plant.",
                        ImageUrl = "https://images.unsplash.com/photo-1538300342682-cf57afb97285?w=800&auto=format&fit=crop&q=80",
                        Category = "General",
                        ProgrammeId = programme?.Id,
                        DisplayOrder = 6,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.GalleryItems.AddRange(gallery);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} Gallery items.", gallery.Count);
            }

            // 10. Seed About Us CMS Pages
            if (!await context.AboutPages.AnyAsync())
            {
                var pages = new List<AboutPage>
                {
                    new()
                    {
                        SectionKey = "what-we-do",
                        Title = "What We Do",
                        Subtitle = "Empowering Communities Through Transparent Welfare & Direct Philanthropy",
                        Content = @"<p class='lead'>Give-AID is a premier unified NGO welfare management platform designed to connect compassionate donors with verified humanitarian causes and grassroots NGOs.</p>
                        <p>We work tirelessly across multiple pillars of human development:</p>
                        <ul>
                            <li><strong>Direct Child Welfare:</strong> Nutrition, healthcare, orphan adoption support, and safe shelter.</li>
                            <li><strong>Education for All:</strong> Scholarships, digital laboratories, and modern learning materials.</li>
                            <li><strong>Special Needs & Disability Care:</strong> Assistive equipment, therapy sessions, and accessibility tools.</li>
                            <li><strong>Women Empowerment:</strong> Vocational skills, micro-entrepreneurship grants, and legal advocacy.</li>
                            <li><strong>Youth Skill Building:</strong> High-demand IT bootcamps, apprenticeships, and employment matchmaking.</li>
                            <li><strong>Elderly Assistance:</strong> Dignified old-age homes, chronic disease medicines, and social care.</li>
                            <li><strong>Emergency Relief:</strong> Rapid disaster logistics, food rations, clean water, and winter kits.</li>
                        </ul>
                        <p>Our digital infrastructure ensures complete financial traceability, instant receipt generation, and real-time project reporting.</p>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    },
                    new()
                    {
                        SectionKey = "our-mission",
                        Title = "Our Mission & Vision",
                        Subtitle = "Building an equitable world where no human being is left behind",
                        Content = @"<h3>Our Mission</h3>
                        <p>To eliminate systemic poverty, ignorance, and vulnerability by orchestrating transparent welfare initiatives, empowering local NGOs, and providing seamless digital giving channels for donors worldwide.</p>
                        <h3 class='mt-4'>Our Vision</h3>
                        <p>A society where every child receives quality education, every disabled individual enjoys dignity and independence, every woman possesses economic autonomy, and every community is resilient against socio-economic crises.</p>
                        <h3 class='mt-4'>Core Values</h3>
                        <div class='row mt-3'>
                            <div class='col-md-6 mb-3'>
                                <div class='p-3 border rounded bg-light'>
                                    <h5><i class='bi bi-shield-check text-success me-2'></i>Transparency</h5>
                                    <p class='mb-0'>100% auditable donation flow with public receipt verification and transparent cause allocations.</p>
                                </div>
                            </div>
                            <div class='col-md-6 mb-3'>
                                <div class='p-3 border rounded bg-light'>
                                    <h5><i class='bi bi-heart text-danger me-2'></i>Compassion</h5>
                                    <p class='mb-0'>Unconditional humanitarian empathy driving every programme and relief expedition.</p>
                                </div>
                            </div>
                            <div class='col-md-6 mb-3'>
                                <div class='p-3 border rounded bg-light'>
                                    <h5><i class='bi bi-people text-primary me-2'></i>Inclusivity</h5>
                                    <p class='mb-0'>Serving every vulnerable demographic regardless of gender, faith, ethnicity, or geography.</p>
                                </div>
                            </div>
                            <div class='col-md-6 mb-3'>
                                <div class='p-3 border rounded bg-light'>
                                    <h5><i class='bi bi-lightning text-warning me-2'></i>Impact & Speed</h5>
                                    <p class='mb-0'>Rapid deployment of funds to maximize real-world outcomes and immediate crisis response.</p>
                                </div>
                            </div>
                        </div>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    },
                    new()
                    {
                        SectionKey = "our-team",
                        Title = "Our Leadership & Advisory Team",
                        Subtitle = "Dedicated humanitarians, architects, and community leaders steering Give-AID",
                        Content = @"<p class='lead text-center mb-5'>Our diverse team brings together decades of experience across public welfare, disaster relief, enterprise technology, and non-profit governance.</p>
                        <div class='row text-center'>
                            <div class='col-md-4 mb-4'>
                                <div class='card border-0 shadow-sm p-4 h-100'>
                                    <img src='https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&auto=format&fit=crop&q=80' class='rounded-circle mx-auto mb-3' style='width:120px;height:120px;object-fit:cover;' alt='Dr. Sarah Mansoor'>
                                    <h5>Dr. Sarah Mansoor</h5>
                                    <p class='text-primary fw-semibold'>President & Co-Founder</p>
                                    <p class='text-muted small'>Over 18 years leading public health initiatives, disaster response taskforces, and UN NGO partnership forums.</p>
                                </div>
                            </div>
                            <div class='col-md-4 mb-4'>
                                <div class='card border-0 shadow-sm p-4 h-100'>
                                    <img src='https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=300&auto=format&fit=crop&q=80' class='rounded-circle mx-auto mb-3' style='width:120px;height:120px;object-fit:cover;' alt='Engr. Bilal Hashmi'>
                                    <h5>Engr. Bilal Hashmi</h5>
                                    <p class='text-primary fw-semibold'>Chief Operations Officer</p>
                                    <p class='text-muted small'>Specialist in relief supply chain logistics, solar micro-utility installations, and field volunteer management.</p>
                                </div>
                            </div>
                            <div class='col-md-4 mb-4'>
                                <div class='card border-0 shadow-sm p-4 h-100'>
                                    <img src='https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=300&auto=format&fit=crop&q=80' class='rounded-circle mx-auto mb-3' style='width:120px;height:120px;object-fit:cover;' alt='Amina Qureshi'>
                                    <h5>Amina Qureshi</h5>
                                    <p class='text-primary fw-semibold'>Director of Youth & Women Welfare</p>
                                    <p class='text-muted small'>Passionate advocate for gender equity, STEM scholarships for young girls, and artisan market access.</p>
                                </div>
                            </div>
                        </div>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    },
                    new()
                    {
                        SectionKey = "career",
                        Title = "Career With Us",
                        Subtitle = "Join our mission-driven team and turn your skills into transformative humanitarian impact",
                        Content = @"<p class='lead'>At Give-AID, our staff and volunteers are our most valuable asset. We foster a collaborative, ethical, and inspiring workplace where passionate people solve meaningful social challenges.</p>
                        <h4 class='mt-4 mb-3'>Why Work at Give-AID?</h4>
                        <div class='row mb-4'>
                            <div class='col-md-4 mb-3'>
                                <div class='p-3 border rounded'>
                                    <h6><i class='bi bi-award text-success me-2'></i>Purpose-Driven Work</h6>
                                    <p class='small text-muted mb-0'>Every code line, logistics route, and strategy directly elevates vulnerable lives.</p>
                                </div>
                            </div>
                            <div class='col-md-4 mb-3'>
                                <div class='p-3 border rounded'>
                                    <h6><i class='bi bi-laptop text-primary me-2'></i>Hybrid Flexibility</h6>
                                    <p class='small text-muted mb-0'>Balanced office and remote workflows designed for high productivity and wellness.</p>
                                </div>
                            </div>
                            <div class='col-md-4 mb-3'>
                                <div class='p-3 border rounded'>
                                    <h6><i class='bi bi-mortarboard text-warning me-2'></i>Continuous Learning</h6>
                                    <p class='small text-muted mb-0'>Generous stipends for certifications, workshops, and leadership development.</p>
                                </div>
                            </div>
                        </div>
                        <h4 class='mt-4 mb-3'>Open Positions</h4>
                        <div class='list-group'>
                            <div class='list-group-item list-group-item-action d-flex justify-content-between align-items-center p-3'>
                                <div>
                                    <h6 class='mb-1'>Field Programme Coordinator - Healthcare & Relief</h6>
                                    <p class='mb-0 small text-muted'><i class='bi bi-geo-alt me-1'></i>Islamabad & Remote Field | Full Time</p>
                                </div>
                                <a href='/Contact' class='btn btn-outline-primary btn-sm'>Apply via Contact</a>
                            </div>
                            <div class='list-group-item list-group-item-action d-flex justify-content-between align-items-center p-3'>
                                <div>
                                    <h6 class='mb-1'>Volunteer Community Manager</h6>
                                    <p class='mb-0 small text-muted'><i class='bi bi-geo-alt me-1'></i>Lahore / Hybrid | Full Time</p>
                                </div>
                                <a href='/Contact' class='btn btn-outline-primary btn-sm'>Apply via Contact</a>
                            </div>
                            <div class='list-group-item list-group-item-action d-flex justify-content-between align-items-center p-3'>
                                <div>
                                    <h6 class='mb-1'>Digital Media & Storytelling Specialist</h6>
                                    <p class='mb-0 small text-muted'><i class='bi bi-geo-alt me-1'></i>Karachi / Remote | Part Time</p>
                                </div>
                                <a href='/Contact' class='btn btn-outline-primary btn-sm'>Apply via Contact</a>
                            </div>
                        </div>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1521737711867-e3b97375f902?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    },
                    new()
                    {
                        SectionKey = "achievements",
                        Title = "Our Achievements & Impact Milestones",
                        Subtitle = "Key milestones achieved through our collective community giving and NGO partnerships",
                        Content = @"<p class='lead text-center mb-5'>Through the steadfast generosity of thousands of donors and dedicated NGO partners, Give-AID has unlocked life-changing results across the nation.</p>
                        <div class='row text-center mb-5'>
                            <div class='col-md-3 mb-3'>
                                <div class='p-4 bg-primary text-white rounded shadow-sm'>
                                    <h2 class='display-6 fw-bold mb-1'>50,000+</h2>
                                    <p class='mb-0'>Individuals Supported</p>
                                </div>
                            </div>
                            <div class='col-md-3 mb-3'>
                                <div class='p-4 bg-success text-white rounded shadow-sm'>
                                    <h2 class='display-6 fw-bold mb-1'>PKR 25M+</h2>
                                    <p class='mb-0'>Disbursed to Verified Causes</p>
                                </div>
                            </div>
                            <div class='col-md-3 mb-3'>
                                <div class='p-4 bg-info text-white rounded shadow-sm'>
                                    <h2 class='display-6 fw-bold mb-1'>120+</h2>
                                    <p class='mb-0'>Programmes Executed</p>
                                </div>
                            </div>
                            <div class='col-md-3 mb-3'>
                                <div class='p-4 bg-warning text-dark rounded shadow-sm'>
                                    <h2 class='display-6 fw-bold mb-1'>45+</h2>
                                    <p class='mb-0'>Partner NGO Networks</p>
                                </div>
                            </div>
                        </div>
                        <h3>Major Programme Highlights</h3>
                        <ul>
                            <li><strong>Clean Drinking Water Initiative:</strong> Established 18 clean water purification plants serving 45,000 villagers daily in drought-hit regions.</li>
                            <li><strong>Bright Future Scholarship Fund:</strong> Fully funded tuition and supplies for 1,200 orphaned and impoverished students from primary through high school.</li>
                            <li><strong>Free Eye & Cataract Clinics:</strong> Restored vision for over 3,400 senior citizens through free laser surgeries and post-op care.</li>
                            <li><strong>Women Entrepreneurship Kits:</strong> Provided 850 industrial sewing machines and digital training to widows and heads of households.</li>
                        </ul>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1511632765486-a01980e01a18?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    },
                    new()
                    {
                        SectionKey = "supporters",
                        Title = "Our Supporters & Donors",
                        Subtitle = "Honoring the visionary institutions, philanthropists, and everyday heroes backing Give-AID",
                        Content = @"<p class='lead'>Every milestone we celebrate is made possible by the heartfelt generosity of our individual donors, family endowments, and institutional CSR champions.</p>
                        <div class='card bg-light border-0 p-4 mb-4'>
                            <h4 class='text-primary'><i class='bi bi-gem me-2'></i>Institutional Philanthropy Circle</h4>
                            <p>We extend our deepest gratitude to our corporate sponsors, educational institutions, and healthcare alliances who provide continuous matching grants, software licenses, logistics equipment, and medical supplies.</p>
                        </div>
                        <h4>How Supporters Make a Direct Difference</h4>
                        <div class='row mt-3'>
                            <div class='col-md-6 mb-3'>
                                <div class='p-3 border rounded bg-white shadow-sm'>
                                    <h6><i class='bi bi-check-circle-fill text-success me-2'></i>Matching Gift Programmes</h6>
                                    <p class='text-muted small mb-0'>Corporate partners match employee contributions 1:1, doubling the impact for children and education causes.</p>
                                </div>
                            </div>
                            <div class='col-md-6 mb-3'>
                                <div class='p-3 border rounded bg-white shadow-sm'>
                                    <h6><i class='bi bi-check-circle-fill text-success me-2'></i>In-Kind Resource Giving</h6>
                                    <p class='text-muted small mb-0'>Supplying ambulances, solar panels, textbooks, food shipments, and surgical equipment directly to partner NGOs.</p>
                                </div>
                            </div>
                        </div>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1559027615-cd4628902d4a?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    },
                    new()
                    {
                        SectionKey = "read-about-us",
                        Title = "Read About Give-AID",
                        Subtitle = "Comprehensive Overview, Governance, Transparency & Operating Principles",
                        Content = @"<p class='lead'>Give-AID was founded on a simple yet profound conviction: that technology, transparency, and empathy can bridge the gap between global generosity and urgent local needs.</p>
                        <h4>Our Model</h4>
                        <p>Unlike traditional charitable trusts where administrative overheads obscure fund distribution, Give-AID operates with a real-time, database-driven allocation system. Donors can select their exact cause—whether Children Welfare, Disabled Persons, Women, Youth, Elderly, or Disaster Relief—and receive an immutable digital receipt with transaction reference tracking.</p>
                        <h4>Rigorous NGO Vetting</h4>
                        <p>Every participating NGO listed on Give-AID undergoes an extensive multi-tier verification process, including validation of government registration certificates, financial audits, executive background checks, and on-ground field efficacy assessments.</p>
                        <h4>Security & Trust</h4>
                        <p>Our payment gateway abstractions (JazzCash, Easypaisa, Debit/Credit Card) follow bank-grade security protocols with zero raw credit card or PIN retention, strict role-based access control, and comprehensive audit trails.</p>",
                        BannerImageUrl = "https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?w=1200&auto=format&fit=crop&q=80",
                        LastModifiedAt = DateTime.UtcNow,
                        ModifiedBy = "System Administrator"
                    }
                };

                context.AboutPages.AddRange(pages);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} About Us CMS pages.", pages.Count);
            }

            // 11. Seed FAQ Categories & FAQs
            if (!await context.FaqCategories.AnyAsync())
            {
                var catGeneral = new FaqCategory { Name = "General Questions", DisplayOrder = 1, IsActive = true };
                var catDonation = new FaqCategory { Name = "Donations & Payment Methods", DisplayOrder = 2, IsActive = true };
                var catProgrammes = new FaqCategory { Name = "Programmes & Volunteering", DisplayOrder = 3, IsActive = true };
                var catTax = new FaqCategory { Name = "Receipts & Security", DisplayOrder = 4, IsActive = true };

                context.FaqCategories.AddRange(catGeneral, catDonation, catProgrammes, catTax);
                await context.SaveChangesAsync();

                var faqs = new List<Faq>
                {
                    new()
                    {
                        FaqCategoryId = catGeneral.Id,
                        Question = "What is Give-AID and how does it work?",
                        Answer = "Give-AID is a centralized digital NGO welfare platform that unites verified non-governmental organizations and donors. You can explore active welfare causes (Children, Education, Women, Elderly, Disabled, etc.), donate securely via JazzCash, Easypaisa, or Card, express interest in upcoming programmes, and invite friends to join.",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new()
                    {
                        FaqCategoryId = catGeneral.Id,
                        Question = "How do you verify partner NGOs?",
                        Answer = "All partner NGOs undergo rigorous compliance checks, including verification of their official government registration, operational history, financial transparency, and physical field inspections before being allowed to raise funds on Give-AID.",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new()
                    {
                        FaqCategoryId = catDonation.Id,
                        Question = "What payment methods are supported on Give-AID?",
                        Answer = "We support multiple secure payment methods including JazzCash Mobile Wallet, Easypaisa Mobile Account, and all major Debit/Credit Cards (Visa, Mastercard, etc.).",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new()
                    {
                        FaqCategoryId = catDonation.Id,
                        Question = "Can I donate anonymously?",
                        Answer = "Yes! When filling out the donation form, simply check the 'Donate Anonymously' checkbox. Your public name will be masked on public leaderboards while still providing you with a private receipt.",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new()
                    {
                        FaqCategoryId = catProgrammes.Id,
                        Question = "How can I register my interest in an upcoming welfare programme?",
                        Answer = "Browse our 'Programmes' page, select any active or upcoming programme, and click the 'I am Interested' button. If you are logged in, your details will be pre-filled. Our coordinators will contact you with logistical updates.",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new()
                    {
                        FaqCategoryId = catTax.Id,
                        Question = "Will I receive an official receipt after making a donation?",
                        Answer = "Yes! Immediately upon payment approval, the system generates an official Give-AID receipt with a unique Receipt Number (GA-YYYYMMDD-XXXX). You can view, print, or download it from your Member Dashboard, and a copy is also emailed to your address.",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new()
                    {
                        FaqCategoryId = catTax.Id,
                        Question = "Are my credit card and mobile wallet credentials stored safely?",
                        Answer = "Give-AID follows strict zero-retention security protocols. We NEVER store raw card numbers, CVVs, card PINs, JazzCash MPINs, or Easypaisa PINs on our servers. All sensitive interactions are processed through encrypted gateway abstractions.",
                        DisplayOrder = 2,
                        IsActive = true
                    }
                };

                context.Faqs.AddRange(faqs);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} FAQs.", faqs.Count);
            }

            // 12. Seed Site Settings
            if (!await context.SiteSettings.AnyAsync())
            {
                var settings = new List<SiteSetting>
                {
                    new() { SettingKey = "SiteName", SettingValue = "Give-AID NGO", Group = "General", Description = "Website Title" },
                    new() { SettingKey = "Tagline", SettingValue = "Welfare & Donation Management System", Group = "General", Description = "Website Tagline" },
                    new() { SettingKey = "ContactEmail", SettingValue = "contact@giveaid.org", Group = "Contact", Description = "Public Contact Email" },
                    new() { SettingKey = "ContactPhone", SettingValue = "+92-51-111-448-324", Group = "Contact", Description = "Public Helpline" },
                    new() { SettingKey = "OfficeAddress", SettingValue = "Give-AID Tower, Sector G-8 Markaz, Islamabad, Pakistan", Group = "Contact", Description = "Physical Address" },
                    new() { SettingKey = "FacebookUrl", SettingValue = "https://facebook.com/giveaidngo", Group = "Social", Description = "Facebook Page" },
                    new() { SettingKey = "TwitterUrl", SettingValue = "https://twitter.com/giveaidngo", Group = "Social", Description = "Twitter / X Profile" },
                    new() { SettingKey = "InstagramUrl", SettingValue = "https://instagram.com/giveaidngo", Group = "Social", Description = "Instagram Profile" },
                    new() { SettingKey = "LinkedInUrl", SettingValue = "https://linkedin.com/company/giveaidngo", Group = "Social", Description = "LinkedIn Profile" },
                    new() { SettingKey = "DefaultCurrency", SettingValue = "PKR", Group = "Finance", Description = "Default System Currency" }
                };

                context.SiteSettings.AddRange(settings);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} Site Settings.", settings.Count);
            }

            // 13. Seed sample initial donation so charts and stats look alive
            if (!await context.Donations.AnyAsync())
            {
                var cause = await context.DonationCauses.FirstOrDefaultAsync();
                if (cause != null && demoUser != null)
                {
                    var sampleDonation = new Donation
                    {
                        UserId = demoUser.Id,
                        CauseId = cause.Id,
                        Amount = 5000m,
                        Currency = "PKR",
                        DonorName = demoUser.FullName,
                        DonorEmail = demoUser.Email!,
                        DonorPhone = demoUser.PhoneNumber,
                        Message = "In loving memory of parents. Keep up the noble work!",
                        Status = DonationStatus.Paid,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    };

                    context.Donations.Add(sampleDonation);
                    await context.SaveChangesAsync();

                    var sampleTxn = new PaymentTransaction
                    {
                        DonationId = sampleDonation.Id,
                        TransactionId = $"JC{DateTime.UtcNow:yyyyMMdd}8892",
                        GatewayTransactionId = $"JC-SBX-SEED-{DateTime.UtcNow.Ticks}",
                        PaymentMethod = PaymentProvider.JazzCash,
                        GatewayName = "JazzCash",
                        Amount = sampleDonation.Amount,
                        Currency = sampleDonation.Currency,
                        Status = DonationStatus.Paid,
                        CreatedAt = sampleDonation.CreatedAt
                    };
                    context.PaymentTransactions.Add(sampleTxn);
                    await context.SaveChangesAsync();

                    var sampleReceipt = new DonationReceipt
                    {
                        DonationId = sampleDonation.Id,
                        ReceiptNumber = $"GA-{DateTime.UtcNow:yyyyMMdd}-0001",
                        DonorName = sampleDonation.DonorName,
                        DonorEmail = sampleDonation.DonorEmail,
                        CauseTitle = cause.Title,
                        Amount = sampleDonation.Amount,
                        Currency = sampleDonation.Currency,
                        PaymentMethod = "JazzCash",
                        TransactionReference = sampleTxn.TransactionId,
                        IssuedAt = sampleDonation.CreatedAt
                    };
                    context.DonationReceipts.Add(sampleReceipt);
                    await context.SaveChangesAsync();
                }
            }

            logger.LogInformation("Database seed finished successfully.");
        }
    }
}
