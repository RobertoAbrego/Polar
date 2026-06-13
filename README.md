
# Parte 1 - Propuesta de Plataforma:
# POLAR - Red Social de Concientización y Acción contra el Cambio Climático

# 1. Descripción General

POLAR es una red social enfocada en la acción climática, que combina gamificación, inteligencia artificial y participación comunitaria para incentivar hábitos sostenibles en la vida cotidiana.

A diferencia de otras plataformas que solo informan, POLAR busca generar impacto real, motivando a los usuarios a cumplir pequeñas acciones diarias que, acumuladas, contribuyen a la reducción del impacto ambiental.

---

# 2. Objetivo

Fomentar la conciencia ambiental y transformar hábitos diarios mediante:

- Acciones simples pero medibles.
- Incentivos digitales (recompensas).
- Participación social y comunitaria.

---

# 3. Problema que aborda

Muchas personas:

- Quieren ayudar al medio ambiente, pero no saben cómo.
- Subestiman el impacto de pequeñas acciones.
- Pierden motivación rápidamente.

POLAR resuelve esto haciendo que el cambio climático sea algo tangible, medible y social.

---

# 4. Solución Propuesta

Una plataforma donde los usuarios:

- Reciben misiones ecológicas diarias.
- Suben evidencia de sus acciones.
- Ganan estrellas (puntos) y suben de nivel.
- Interactúan con otros usuarios.

---

# 5. Funcionalidades Principales

## 5.1 Sistema de Misiones

### Ejemplos

- Apagar luces innecesarias.
- Reducir uso de electrodomésticos de alto consumo.
- Recoger basura en espacios públicos.
- Reducir consumo de plástico.

Las misiones pueden ser:

- Diarias.
- Semanales.
- Comunitarias.

---

## 5.2 Inteligencia Artificial

La IA cumple un rol clave:

- Genera misiones personalizadas según hábitos del usuario.
- Detecta patrones de comportamiento.
- Sugiere mejoras progresivas.
- Ajusta la dificultad para mantener la motivación.

---

## 5.3 Sistema de Recompensas

- Estrellas ⭐ por cada misión completada.
- Niveles de usuario.
- Insignias ecológicas.
- Rankings (amigos, ciudad y global).

---

## 5.4 Sistema de Validación

Para evitar fraude:

- Subida de fotos y videos como evidencia.
- Validación comunitaria.
- IA que detecta inconsistencias.

---

## 5.5 Propuestas Comunitarias

Los usuarios pueden:

- Crear nuevas misiones.
- Proponer retos colectivos.
- Unirse a campañas ambientales.

Ejemplo:

> Recolectar 100 kg de basura en una colonia en una semana.

---

## 5.6 Elemento Social

- Perfil de usuario.
- Seguidores.
- Feed de actividades.
- Comentarios y reacciones.

---

# 6. Elemento Diferenciador

POLAR no es solo una red social, es un ecosistema de acción climática:

- Gamificación + IA + impacto real.
- Enfoque en acciones pequeñas pero constantes.
- Validación social mediante evidencia.

---

# 7. Impacto Esperado

- Reducción del consumo energético doméstico.
- Mayor participación en actividades ecológicas.
- Cambio de hábitos a largo plazo.
- Conciencia ambiental en jóvenes (principal público objetivo).

---
<br> <br>

# Parte 2 - Restauración de la Base de Datos

Para poder restaurar la base de datos necesitamos disponer del archivo de respaldo correspondiente.
Para eso puedes decargarlo [AQUI](https://drive.google.com/drive/folders/19axkAoKp6EHJ1DwlDsugGSFtMlPY__Pf)

## Paso 1

Abrimos una terminal.

## Paso 2

```bash
docker stop polar_app_dev
```

### ¿Qué hace?

Detiene el contenedor donde está ejecutándose la aplicación web.

### ¿Por qué se hace primero?

Si la aplicación continúa conectada a la base de datos mientras se realiza la restauración, pueden ocurrir problemas como:

- Mantener conexiones activas.
- Bloquear archivos.
- Interrumpir el proceso de restauración.
- Generar errores de acceso.

---

## Paso 3

```bash
docker exec -it db2 bash
```

### ¿Qué hace?

Abre una terminal interactiva dentro del contenedor llamado `db2`.

### Desglose

- `docker exec` → Ejecuta un comando dentro de un contenedor.
- `-it` → Modo interactivo.
- `db2` → Nombre del contenedor.
- `bash` → Abre una shell.

Ahora nos encontramos dentro del servidor IBM Db2.

---

## Paso 4

```bash
su - db2inst1
```

### ¿Qué hace?

Cambia la sesión al usuario propietario de la instancia Db2.

### ¿Por qué?

Muchos comandos administrativos de IBM Db2 deben ejecutarse mediante el usuario interno `db2inst1`.

---

## Paso 5

```bash
db2 force applications all
```

### ¿Qué hace?

Desconecta todas las aplicaciones conectadas a la base de datos.

### ¿Por qué es necesario?

Si existen conexiones activas provenientes de:

- La aplicación web.
- Otras terminales.
- Procesos internos.

IBM Db2 no permitirá ejecutar operaciones de restauración.

Este comando elimina todas las sesiones activas.

---

## Paso 6

```bash
db2 deactivate db POLAR
```

### ¿Qué hace?

Desactiva la base de datos POLAR en memoria.

### Importante

Este comando no elimina información.

Simplemente desmonta temporalmente la base de datos, de manera similar a cerrar un archivo antes de reemplazarlo.

---

## Paso 7

```bash
db2 restore db POLAR from /database/backup taken at 20260504013032 replace existing
```

### ¿Qué hace?

Restaura la base de datos POLAR utilizando un respaldo específico.

### Desglose

#### `restore db POLAR`

Indica que se restaurará la base de datos POLAR.

#### `from /database/backup`

Especifica la carpeta donde se encuentra almacenado el respaldo.

#### `taken at 20260504013032`

Selecciona el backup exacto que se utilizará.

Formato:

```
AAAAMMDDHHMMSS
```

En este caso:

- Año: 2026
- Mes: 05
- Día: 04
- Hora: 01:30:32

#### `replace existing`

Sobrescribe la base de datos actual con la copia restaurada.

---

## Paso 8

```bash
db2 connect to POLAR
```

### ¿Qué hace?

Abre una conexión con la base de datos restaurada para verificar que el proceso finalizó correctamente.

Si aparece una respuesta similar a:

```
Database Connection Information
```

significa que la restauración fue exitosa.

---

## Paso 9

Una vez verificada la restauración, volvemos a iniciar el servidor web.

```bash
docker start polar_app_dev
```

### ¿Qué hace?

Vuelve a poner en funcionamiento la aplicación web para que los usuarios puedan acceder nuevamente al sistema.

---

# Propuesta de Plataforma:
# POLAR - Red Social de Concientización y Acción contra el Cambio Climático

...