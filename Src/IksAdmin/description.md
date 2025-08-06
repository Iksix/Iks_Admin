IksAdmin | Core

Ядро собирающее в себе весь проект + точка входа в плагин


Связи в прокте

IksAdmin -> All

IksAdmin.Api -> IksAdmin.Application

IksAdmin.Api.Legacy -> IksAdmin.Application

IksAdmin.Application -> IksAdmin.Entities

IksAdmin.Infrastructure.* -> IksAdmin.Entities (Подключается через DI)