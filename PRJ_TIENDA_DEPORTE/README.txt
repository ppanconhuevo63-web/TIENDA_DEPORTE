TIENDA DEPORTIVA MVC - .NET 10

1. Abrir la solucion TIENDA_DEPORTE.sln en Visual Studio 2026.
2. Restaurar paquetes NuGet.
3. Ejecutar el script BaseDeDatos/TIENDA_DEPORTE_V6.sql en SQL Server.
4. Revisar y ajustar la cadena de conexion en appsettings.json si tu servidor o credenciales son distintas.
5. Ejecutar el proyecto.

Usuarios iniciales:
- ADMIN PRINCIPAL: adminprincipal / admin123
- ADMINISTRADOR: admin / admin123
- CAJERO: cajero / cajero123

Cambios aplicados:
- Roles fijos del sistema: ADMIN, ADMINISTRADOR y CAJERO.
- Ya no se crean roles nuevos desde la vista Rol.
- El campo Rol en Usuario se calcula automaticamente segun el empleado seleccionado.
- En Crear Usuario solo aparecen empleados activos sin usuario registrado.
- El ADMINISTRADOR solo puede crear usuarios CAJERO.
- El ADMINISTRADOR no puede eliminar al ADMIN PRINCIPAL ni eliminarse a si mismo.
- Al editar su propio usuario, el ADMINISTRADOR tiene bloqueado el cambio de empleado.

Notas:
- El rol ADMIN tiene acceso total a todos los modulos.
- El rol ADMINISTRADOR mantiene acceso a gestion, pero con restricciones en usuarios y roles.
- El rol CAJERO solo ve Clientes, Venta y Detalle Venta.
