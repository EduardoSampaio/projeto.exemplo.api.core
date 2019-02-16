﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Projeto.Livraria.Dados.Interfaces;
using Projeto.Livraria.Dados.Repositorios;
using Projeto.Livraria.Dados.Source;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;

namespace Projeto.Livaria.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        private readonly ILogger _logger;
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            InitializeDependencyInjection(services);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Api Livros", Version = "v1" });
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddApiVersioning(o => o.ApiVersionReader = new HeaderApiVersionReader("api-version"));
        }

        private void InitializeDependencyInjection(IServiceCollection services)
        {
            var connection = Configuration["MySqlConnection:MysqlConnectionString"];
            services.AddDbContext<MySqlContext>(opt => opt.UseMySQL(connection));

            services.AddScoped(typeof(IRepositorio<,>), typeof(Repositorio<,>));
            services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
            services.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
            services.AddScoped<ILivroRepositorio, LivroRepositorio>();      
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                InitializeDatabase();

                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Livros v1");
            });

            app.UseMvc();
        }

        private void InitializeDatabase()
        {
            try
            {
                var evolveConnection = new MySqlConnection(Configuration["MySqlConnection:MysqlConnectionString"]);
                var evolve = new Evolve.Evolve("evolve.json", evolveConnection, msg => _logger.LogInformation(msg))
                {
                    Locations = new List<string> { "db/migrations" },
                    IsEraseDisabled = true

                };
                evolve.Migrate();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Database migration failed.", ex);
                throw;
            }

        }
    }
}