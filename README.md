# 📱 Sistema de Verificación IMEI - Backend API

API REST desarrollada en **ASP.NET Core (.NET)** para la verificación, gestión segura de IMEIs y administración de usuarios asociados a empresas.

El sistema implementa autenticación basada en JWT, control de acceso por roles, encriptación de datos sensibles y arquitectura en capas para garantizar mantenibilidad, seguridad y escalabilidad.

---

## 🚀 Stack Tecnológico

- ASP.NET Core (.NET 6/7)
- Entity Framework Core
- SQL Server
- JWT (JSON Web Token)
- BCrypt (Hash seguro de contraseñas)
- AES (CBC + PKCS7) para encriptación
- Docker
- Render (deployment preparado)

---

## 🏗 Arquitectura del Proyecto

El proyecto sigue una arquitectura en capas para separación de responsabilidades:

### Beneficios de esta arquitectura

- Desacoplamiento entre capas
- Código mantenible y escalable
- Mayor facilidad para testing
- Separación clara de responsabilidades

---

## 🔐 Seguridad Implementada

### 🔑 Autenticación y Autorización
- Login con validación segura de credenciales.
- Generación de JWT firmado con clave simétrica.
- Expiración configurable del token.
- Claims personalizados:
  - `userId`
  - `rol`
  - `empresaId`
- Integración con `[Authorize(Roles = "...")]`.

### 🔒 Protección de Contraseñas
- Hashing mediante BCrypt.
- No se almacenan contraseñas en texto plano.

### 🔐 Encriptación de Datos Sensibles
- AES en modo CBC.
- Padding PKCS7.
- Generación de hash SHA256 adicional.

---

## 📌 Endpoints Principales

### 🔑 Autenticación

**POST** `/api/auth/login`

Request:

```json
{
  "username": "admin",
  "password": "123456"
}
