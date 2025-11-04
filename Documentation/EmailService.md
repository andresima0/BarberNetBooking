# 📧 Guia de Configuração de E-mail para BarberNet

## 🎯 Funcionalidades Implementadas

O sistema agora envia e-mails automáticos em HTML para:
- ✅ **Confirmação de agendamento** (quando cliente agenda)
- ❌ **Cancelamento de agendamento** (quando admin cancela)
- 🔄 **Remarcação de agendamento** (quando admin remarca)
- 🗑️ **Exclusão múltipla** (envia cancelamento para cada agendamento deletado)

---

## 📝 Passo a Passo - Configuração com Gmail

### 1️⃣ Criar Senha de App no Gmail

Como o Gmail não permite mais usar a senha normal para aplicações, você precisa criar uma **Senha de App**:

1. Acesse: https://myaccount.google.com/security
2. Ative a **Verificação em duas etapas** (se ainda não tiver)
3. Depois de ativar, volte para Segurança
4. Procure por **"Senhas de app"** ou acesse: https://myaccount.google.com/apppasswords
5. Selecione:
    - **App:** Outro (nome personalizado)
    - **Nome:** BarberNet
6. Clique em **Gerar**
7. **Copie a senha de 16 dígitos** (sem espaços)

### 2️⃣ Configurar o appsettings.json

Edite o arquivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=barbernet.db"
  },
  "Admin": {
    "Pin": "seu-pin-de-acesso"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "seuemail@gmail.com",
    "SmtpPassword": "xxxx xxxx xxxx xxxx",
    "FromEmail": "seuemail@gmail.com",
    "FromName": "BarberNet"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Substitua:**
- `seuemail@gmail.com` → Seu e-mail Gmail
- `xxxx xxxx xxxx xxxx` → A senha de app gerada (pode ter espaços)

### 3️⃣ Proteger Senha em Produção

**⚠️ IMPORTANTE:** Nunca commite senhas no Git!

#### Opção A: User Secrets (Desenvolvimento)
```bash
dotnet user-secrets init
dotnet user-secrets set "Admin:Pin" "seu-pin-de-acesso"  
dotnet user-secrets set "Email:SmtpUser" "seuemail@gmail.com"
dotnet user-secrets set "Email:SmtpPassword" "sua-senha-de-app"
dotnet user-secrets list
```

#### Opção B: Variáveis de Ambiente (Produção)
```bash
export Email__SmtpUser="seuemail@gmail.com"
export Email__SmtpPassword="sua-senha-de-app"
```

---

## 🔧 Outras Provedoras de E-mail

### Outlook/Hotmail
```json
"Email": {
  "SmtpHost": "smtp-mail.outlook.com",
  "SmtpPort": "587",
  "SmtpUser": "seuemail@outlook.com",
  "SmtpPassword": "sua-senha",
  "FromEmail": "seuemail@outlook.com",
  "FromName": "BarberNet"
}
```

### SendGrid (Recomendado para Produção)
```json
"Email": {
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": "587",
  "SmtpUser": "apikey",
  "SmtpPassword": "SG.sua-api-key",
  "FromEmail": "noreply@seudominio.com",
  "FromName": "BarberNet"
}
```

### Mailgun
```json
"Email": {
  "SmtpHost": "smtp.mailgun.org",
  "SmtpPort": "587",
  "SmtpUser": "postmaster@seudominio.mailgun.org",
  "SmtpPassword": "sua-senha",
  "FromEmail": "noreply@seudominio.com",
  "FromName": "BarberNet"
}
```

---

## 🧪 Testando o Envio de E-mails

### 1. Fazer um agendamento pela página principal
- Acesse: http://localhost:5082
- Preencha o formulário
- Use um e-mail válido que você tenha acesso
- Aguarde a mensagem de sucesso
- Verifique sua caixa de entrada (e spam)

### 2. Verificar logs
O sistema loga todas as tentativas de envio:
```
info: BarberNetBooking.Services.EmailService[0]
      E-mail enviado com sucesso para cliente@email.com
```

Se houver erro:
```
fail: BarberNetBooking.Services.EmailService[0]
      Erro ao enviar e-mail para cliente@email.com
      System.Net.Mail.SmtpException: Authentication failed
```

---

## 🎨 Templates de E-mail

Os e-mails são enviados em **HTML responsivo** com:
- ✅ Design moderno e profissional
- ✅ Cores personalizáveis
- ✅ Informações organizadas
- ✅ Compatível com todos os clientes de e-mail
- ✅ Logos e branding

### Exemplo de E-mail de Confirmação:
```
🎉 Agendamento Confirmado!
Seu horário foi reservado com sucesso

📋 Detalhes do Agendamento
─────────────────────────────
Serviço:  Corte masculino
Barbeiro: Luiz
Data:     15/11/2025
Horário:  14:00
Duração:  30 minutos
Valor:    R$ 45,00

⏰ Importante: Por favor, chegue com 5 minutos de antecedência.
```

---

## 🔍 Troubleshooting

### Erro: "Authentication failed"
- ✅ Verifique se a senha de app está correta
- ✅ Confirme que a verificação em duas etapas está ativa
- ✅ Tente gerar uma nova senha de app

### Erro: "The SMTP server requires a secure connection"
- ✅ Confirme que `SmtpPort` é `587`
- ✅ O código já usa `EnableSsl = true`

### E-mail não chega
- ✅ Verifique a pasta de SPAM
- ✅ Confirme se o e-mail do destinatário está correto
- ✅ Verifique os logs da aplicação
- ✅ Alguns provedores bloqueiam e-mails de IPs residenciais

### Limites do Gmail
- **500 e-mails/dia** (conta gratuita)
- **2000 e-mails/dia** (Google Workspace)
- Se precisar enviar mais, use SendGrid ou Mailgun

---

## 🚀 Melhorias Futuras (Opcionais)

1. **Fila de e-mails:** Use Hangfire ou Azure Queue Storage
2. **Tracking de abertura:** Pixel invisível
3. **Templates customizáveis:** Editor no painel admin
4. **Notificações SMS:** Integração com Twilio
5. **WhatsApp:** Integração com API oficial
6. **Agendamento de lembretes:** E-mail 24h antes

---

## 📊 Monitoramento

Para produção, considere:
- **Application Insights** (Azure)
- **Sentry** (erro tracking)
- **SendGrid Analytics** (métricas de e-mail)

---

## ✅ Checklist de Implementação

- [ ] Ativar verificação em duas etapas no Gmail
- [ ] Gerar senha de app
- [ ] Configurar appsettings.json
- [ ] Testar envio de e-mail
- [ ] Verificar se chegou na caixa de entrada
- [ ] Configurar user secrets ou variáveis de ambiente
- [ ] Adicionar .gitignore para appsettings.json (se necessário)
- [ ] Documentar para outros desenvolvedores