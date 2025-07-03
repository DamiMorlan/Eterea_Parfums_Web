using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Eterea_Parfums_Desktop
{
    public static class PasswordHelper
    {
        // Método para crear el hash de la contraseña usando PBKDF2
        // Genera un hash seguro utilizando PBKDF2
        public static string CrearHash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("La contraseña no puede ser nula o vacía.", nameof(password));

            // Genera un salt de 16 bytes
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            // Genera el hash de la contraseña con 10,000 iteraciones
            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                hash = pbkdf2.GetBytes(20); // Se esperan 20 bytes
            }

            // Comprobaciones de seguridad
            if (salt.Length != 16)
                throw new Exception("El salt generado no tiene 16 bytes.");
            if (hash.Length != 20)
                throw new Exception("El hash generado no tiene 20 bytes.");

            // Combina el salt y el hash en un solo arreglo
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, salt.Length);
            Array.Copy(hash, 0, hashBytes, 16, hash.Length);

            // Convierte a Base64 para almacenar en la base de datos
            return Convert.ToBase64String(hashBytes);
        }



        // Método para verificar la contraseña comparando el hash ingresado con el almacenado
        public static bool VerificarPassword(string passwordIngresada, string storedHash)
        {
            if (string.IsNullOrEmpty(passwordIngresada) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                // Extrae el salt (primeros 16 bytes)
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                // Genera el hash de la contraseña ingresada usando el mismo salt y configuración
                using (var pbkdf2 = new Rfc2898DeriveBytes(passwordIngresada, salt, 10000))
                {
                    byte[] hash = pbkdf2.GetBytes(20);
                    // Compara byte a byte el hash generado con el almacenado
                    for (int i = 0; i < 20; i++)
                    {
                        if (hashBytes[i + 16] != hash[i])
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                // Maneja errores como formato inválido de Base64, etc.
                return false;
            }
        }

    }
}


