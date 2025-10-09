
# BarberNet — Sistema de Agendamento para Barbearias

## Visão Geral

O **BarberNet** é um sistema simples de agendamento desenvolvido em **ASP.NET Core** com **Razor Pages** e **Entity Framework Core** (usando **SQLite** como banco de dados). O sistema permite que clientes agendem serviços em uma barbearia e que administradores (barbeiros) gerenciem esses agendamentos e os serviços oferecidos. A aplicação possui uma página pública para clientes e uma área administrativa para barbeiros.

### Funcionalidades:

1. **Página de Agendamento (Cliente)**:

    * O cliente pode visualizar os serviços disponíveis, escolher o barbeiro, selecionar a data e o horário, e confirmar o agendamento.

2. **Área Administrativa (Admin)**:

    * A área admin é protegida por um **PIN**, e o acesso é feito através da página **/Admin/Login**.
    * O administrador pode gerenciar os **serviços** (criar, editar, ativar/desativar), **barbeiros** (cadastrar e editar), **agendamentos** (visualizar, cancelar, remarcar) e a **disponibilidade** dos barbeiros.
    * A página **/Admin/Index** exibe um dashboard com a quantidade de agendamentos de hoje e próximos 7 dias, além de um resumo por barbeiro.

## Tecnologias Usadas

* **ASP.NET Core 8**
* **Entity Framework Core** (com SQLite como banco de dados)
* **Razor Pages** para o front-end
* **Cookies** para autenticação simples do administrador

## Banco de Dados e Entity Framework Core

O **Entity Framework Core** (EF Core) é uma biblioteca de mapeamento objeto-relacional (ORM) que simplifica a interação com o banco de dados em aplicações .NET. Neste projeto, utilizamos **SQLite** como banco de dados para armazenamento dos dados.

### Banco de Dados: SQLite

O SQLite é um banco de dados leve e autossuficiente, ideal para aplicativos que não exigem um servidor de banco de dados separado. Ele armazena os dados em um único arquivo no disco, simplificando o processo de configuração e execução.

O banco de dados **SQLite** será criado automaticamente na pasta raiz do projeto, no arquivo `barbernet.db`. O EF Core se conecta ao banco e gera as tabelas automaticamente com base nas migrações criadas e no modelo definido.

### Configuração do Entity Framework Core

O **EF Core** é configurado no arquivo `Program.cs`, onde é definida a conexão com o banco de dados e a injeção de dependências para o contexto de dados.

Em `Program.cs`, a configuração do EF Core é feita da seguinte forma:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Essa configuração estabelece que a aplicação usará SQLite, com a string de conexão definida em `appsettings.json`. Quando o projeto for executado pela primeira vez, o EF Core criará automaticamente o banco de dados e as tabelas.

### Migrações do EF Core

As **migrações** são uma forma de atualizar o banco de dados sempre que o modelo de dados for alterado. Elas permitem que você acompanhe as mudanças no modelo e no esquema do banco de dados.

* **Criar migração**: Para criar uma migração, execute o comando:

  ```bash
  dotnet ef migrations add <NomeMigração>
  ```
* **Aplicar migração**: Para aplicar as migrações e atualizar o banco de dados, execute o comando:

  ```bash
  dotnet ef database update
  ```

O EF Core cria automaticamente uma pasta `Migrations` contendo os arquivos de migração. O arquivo `AppDbContextModelSnapshot.cs` mantém uma "foto" do estado atual do banco de dados para comparação com as migrações futuras.

## Estrutura de Arquivos e Diretórios

O projeto possui a seguinte estrutura:

```
BarberNetBooking/
├─ Program.cs                        // Configuração do app (DB, DI, etc.)
├─ appsettings.json                  // Configurações (incluindo PIN do Admin)
├─ Data/
│  ├─ AppDbContext.cs              // Configuração do banco de dados (DbSets)
│  └─ DbInitializer.cs             // População inicial do banco (seed)
├─ Infrastructure/
│  └─ AdminAuthorizeAttribute.cs  // Autenticação via PIN do Admin (cookie)
├─ Services/
│  └─ SlotService.cs              // Serviço para gerar horários disponíveis
│  └─ AvailabilityService.cs
├─ Models/
│  ├─ Service.cs                  // Modelo de serviços (nome, preço, duração, ativo)
│  ├─ Barber.cs                   // Modelo de barbeiros
│  ├─ Appointment.cs               // Modelo de agendamento (status, cliente, data/hora)
│  ├─ AppointmentStatus.cs         // Enum de status de agendamento (Confirmado/Cancelado)
│  ├─ BarberWorkingHour.cs         // Horário de trabalho do barbeiro
│  ├─ BarberTimeOff.cs             // Folgas do barbeiro
│  └─ ShopSetting.cs               // Configurações da barbearia (ex.: intervalo de agendamento)
├─ Pages/
│  ├─ Index.cshtml                // Página de agendamento para clientes
│  ├─ Shared/
│  │  ├─ _Layout.cshtml          // Layout principal (menu Agendar/Entrar Admin)
│  │  └─ _AdminLayout.cshtml      // Layout Admin
│  └─ Admin/
│     ├─ Login.cshtml             // Aba e página de login do Admin
│     ├─ Logout.cshtml            // Aba para sair da Página de login do Admin
│     ├─ Index.cshtml             // Dashboard do Admin (quantidade de agendamentos)
│     ├─ Services/
│     │  ├─ Index.cshtml          // CRUD de serviços
│     │  └─ _Form.cshtml          // Formulário para criar/editar serviços
│     ├─ Barbers/
│     │  └─ Index.cshtml          // CRUD de barbeiros
│     ├─ Appointments/
│     │  ├─ Index.cshtml          // Visualização, cancelamento e remarcação de agendamentos
│     │  └─ _Filters.cshtml       // Filtros de busca de agendamentos
│     ├─ Availability/
│     │  ├─ Index.cshtml          // Gestão de horários de trabalho dos barbeiros
│     │  └─ _WeeklyRow.cshtml     // Formulário para configurar horário semanal
│     └─ Settings/
│        └─ Index.cshtml          // Configurações gerais (ex.: intervalo de tempo entre agendamentos)
├─ wwwroot/
│  ├─ css/
│  │  └─ site.css                // Estilo global da aplicação
│  ├─ img/
│  │  └─ logo.png              // Logo da barbearia
│  └─ favicon.ico                 // Ícone da página
└─ Migrations/
   ├─ ..._InitialCreate.cs  // Migração inicial (tabelas e colunas)
   │ └─ ..._InitialCreate.Designer.cs
   ├─ ..._UniqueAppointmentsIndex.cs
   │ └─ ..._UniqueAppointmentIndex.Designer.cs
   ├─ ..._MapTimeOnlyToTricks.cs
   │ └─ ..._MapTimeOnlyToTricks.Designer.cs
   └─ AppDbContextModelSnapshot.cs // Snapshot do banco de dados
```

## Como Rodar o Projeto

### Pré-requisitos

* **.NET SDK 8.0** ou superior
* **SQLite** configurado no projeto (não há necessidade de instalação adicional, o banco é criado automaticamente).

### Passos para rodar

1. **Clone ou baixe o repositório**.
2. Abra o terminal/PowerShell e vá até a pasta do projeto.
3. Execute o comando para restaurar as dependências:

   ```bash
   dotnet restore
   ```
4. Para gerar as migrações e criar o banco de dados, rode:

   ```bash
   dotnet ef database update
   ```
5. Rode o projeto:

   ```bash
   dotnet run
   ```
6. Acesse a aplicação:

   https://localhost:5082


## Funcionalidades do Admin

* **Login Admin**: Acesso à área administrativa via PIN.
* **Serviços**: CRUD de serviços oferecidos (nome, preço, duração).
* **Barbeiros**: CRUD de barbeiros.
* **Agendamentos**: Visualização, cancelamento e remarcação de agendamentos.
* **Disponibilidade**: Configuração de horários de trabalho e folgas dos barbeiros.
* **Configurações**: Definição de configurações gerais, como o intervalo entre agendamentos.

## Migrações

* Para criar novas migrações após alterações no modelo, utilize:

  ```bash
  dotnet ef migrations add <NomeMigração>
  dotnet ef database update
  ```

## Observações

* O banco de dados **SQLite** é criado automaticamente e armazenado no arquivo `barbernet.db` na raiz do projeto.
* Para rodar a aplicação em produção, você pode configurar um banco de dados **PostgreSQL** em vez do SQLite.