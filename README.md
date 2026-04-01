<div align="center">

<img src="https://img.shields.io/badge/Forssa-Learning%20Platform-6366f1?style=for-the-badge&logo=graduation-cap&logoColor=white" alt="Forssa" height="40"/>

# ⚙️ Forssa — Backend

### *Powering Accessible Education at Scale*

[![.NET](https://img.shields.io/badge/.NET-9-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Azure](https://img.shields.io/badge/Deployed%20on-Azure-0078D4?style=flat-square&logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/)
[![Supabase](https://img.shields.io/badge/Database-Supabase-3ECF8E?style=flat-square&logo=supabase&logoColor=white)](https://supabase.com/)
[![Stripe](https://img.shields.io/badge/Payments-Stripe-635BFF?style=flat-square&logo=stripe&logoColor=white)](https://stripe.com/)

<br/>

[🌐 Live Demo](https://forssa-frontend.vercel.app/) &nbsp;·&nbsp; [🎨 Frontend Repo](https://github.com/SherifAhmedd/Forssa-Frontend) &nbsp;·&nbsp; [📸 Screenshots](https://drive.google.com/drive/folders/1w3Pcc5fuSLJEhayRsv46X6_6XFh0JPzW?usp=sharing)

<br/>

👤 **[Sherif Ahmed](https://github.com/SherifAhmedd)** &nbsp;·&nbsp; 👤 **[Rwan Ashraf](https://github.com/RwanAshrafff)**

</div>

---

## 📌 Overview

**Forssa** is a full-stack learning platform inspired by Udemy and Coursera, built with a mission: **make quality education accessible to everyone**, especially students who cannot afford paid courses.

This repository contains the **REST API backend** built with **.NET**, hosted on **Microsoft Azure**, and powered by **Supabase** for data and authentication.

> 🎯 *Every API endpoint serves a purpose: democratizing education.*

---

## 🏗️ System Architecture

```
┌─────────────────┐       ┌──────────────────────┐       ┌──────────────┐
│   Next.js App   │──────▶│   .NET REST API       │──────▶│  Supabase DB │
│   (Vercel)      │◀──────│   (Azure App Service)  │◀──────│  + Storage   │
└─────────────────┘       └──────────────────────┘       └──────────────┘
                                     │
                                     ▼
                            ┌──────────────────┐
                            │  Stripe Webhooks  │
                            │  (Payment Events) │
                            └──────────────────┘
```

---

## ✨ Core Features

<table>
<tr>
<td width="50%" valign="top">

### 🎓 Learning Platform
- Course creation, publishing & enrollment
- Module & lesson management
- Video upload & storage (Supabase Storage)
- Progress tracking per lesson
- Certificate generation (PDF)
- Learning streaks & points system

</td>
<td width="50%" valign="top">

### 🔐 Auth & Security
- Google OAuth + Email/Password (via Supabase)
- JWT-based session management
- Role-Based Access Control (RBAC):
  - `Student` | `Instructor` | `Admin`
- Row Level Security (RLS) in Supabase
- Backend role validation on all endpoints

</td>
</tr>
<tr>
<td width="50%" valign="top">

### 💳 Payments
- Stripe Checkout session creation
- Webhook processing for payment events
- Enrollment confirmation post-payment
- Financial aid application management

</td>
<td width="50%" valign="top">

### 🛡️ Admin Controls
- Approve/reject instructor accounts
- Approve/reject financial aid requests
- Moderate student success stories
- Category & content management

</td>
</tr>
</table>

---

## 🏗️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | ASP.NET Core (.NET 9) |
| **Language** | C# |
| **Database** | Supabase (PostgreSQL) |
| **Authentication** | Supabase Auth |
| **File Storage** | Supabase Storage |
| **Payments** | Stripe |
| **Deployment** | Microsoft Azure App Service |

---

## 🗄️ Database Design

```
users (Supabase Auth)
  └── profiles          (role: student | instructor | admin)
        └── courses
              └── modules
                    └── lessons
        └── enrollments ──▶ courses
        └── transactions ──▶ payment tracking
        └── financial_aid
        └── success_stories
```

### Core Tables

| Table | Description |
|-------|-------------|
| `profiles` | Extended user data + role management |
| `courses` | Course metadata, pricing, thumbnail |
| `modules` | Course sections / chapters |
| `lessons` | Individual lesson content & video URLs |
| `enrollments` | Student ↔ Course relationship |
| `transactions` | Stripe payment records |
| `financial_aid` | Aid applications & approval status |
| `success_stories` | Student testimonials pending admin review |

---

## 🔐 Authentication & Authorization

All protected routes require a valid **Supabase JWT** in the `Authorization` header:

```
Authorization: Bearer <supabase_jwt_token>
```

Role enforcement is applied at the controller level:

```csharp
[Authorize(Roles = "Instructor")]
[HttpPost("courses")]
public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto) { ... }
```

---

## 👥 Follow System & Home Feed

Students and instructors can follow each other. The **Home Feed** aggregates activity from followed users in real time.

### API Endpoints — `api/follows`

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/follows/{targetUserId}` | Follow a user |
| `DELETE` | `/api/follows/{targetUserId}` | Unfollow a user |
| `GET` | `/api/follows/following` | Get list of users I follow |
| `GET` | `/api/follows/followers` | Get list of my followers |
| `GET` | `/api/follows/home-feed` | Get personalized home feed |
| `GET` | `/api/follows/users/{userId}/followers` | Get a user's followers |
| `GET` | `/api/follows/users/{userId}/following` | Get a user's following list |
| `GET` | `/api/follows/users/{userId}/connections` | Get follow connection status |

### Home Feed Response

The `home-feed` endpoint returns a merged, chronologically sorted activity stream:

```json
{
  "newCourses": [{ "courseId": "...", "courseTitle": "...", "instructorName": "..." }],
  "newCertificates": [{ "studentName": "...", "courseTitle": "...", "completedAt": "..." }],
  "newFollowers": [{ "userId": "...", "fullName": "...", "followedAt": "..." }]
}
```

> All endpoints require a valid **JWT** in the `Authorization` header.

---

## 💳 Payment Flow

```
POST /api/payments/checkout
       ↓
Creates Stripe Checkout Session
       ↓
User redirected to Stripe
       ↓
POST /api/payments/webhook  ← Stripe notifies backend
       ↓
Payment confirmed → Enrollment created ✅
```

---

## 📁 File Storage

All media is stored in **Supabase Storage** buckets:

| Bucket | Contents |
|--------|----------|
| `course-thumbnails` | Course cover images |
| `lesson-videos` | Uploaded lesson videos |
| `certificates` | Generated PDF certificates |
| `documents` | Financial aid & misc files |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A [Supabase](https://supabase.com/) project
- A [Stripe](https://stripe.com/) account
- (Optional) [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/) for deployment

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/RwanAshrafff/Forssa-Backend.git
cd Forssa-Backend

# 2. Restore dependencies
dotnet restore

# 3. Configure environment variables (see below)

# 4. Run the application
dotnet run
```

The API will be available at `http://localhost:5000`.

### Environment Variables

All secrets are managed via **environment variables** (no `appsettings.json` secrets):

```env
SUPABASE_URL=your_supabase_project_url
SUPABASE_SERVICE_ROLE_KEY=your_supabase_service_role_key
SUPABASE_JWT_SECRET=your_supabase_jwt_secret

STRIPE_SECRET_KEY=your_stripe_secret_key
STRIPE_WEBHOOK_SECRET=your_stripe_webhook_secret

FRONTEND_URL=https://forssa-frontend.vercel.app
```

---

## 📂 Project Structure

```
Forssa-Backend/
├── Controllers/          # API route handlers
│   ├── AuthController.cs
│   ├── CoursesController.cs
│   ├── EnrollmentsController.cs
│   ├── PaymentsController.cs
│   └── AdminController.cs
├── Services/             # Business logic layer
├── Models/               # Domain models & DTOs
├── Middleware/           # Auth & error handling middleware
├── Program.cs            # App entry point & DI config
└── appsettings.json      # Non-secret configuration only
```

---

## ☁️ Deployment (Azure)

The backend is deployed on **Azure App Service**:

1. **Build** the release artifact:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Configure** Azure App Service environment variables via the Azure Portal or CLI:
   ```bash
   az webapp config appsettings set --name <app-name> \
     --resource-group <rg-name> \
     --settings SUPABASE_URL="..." STRIPE_SECRET_KEY="..."
   ```

3. **Deploy** using GitHub Actions CI/CD or Azure CLI.

---

## 🔐 Security Practices

| Practice | Implementation |
|----------|---------------|
| 🔒 Secret management | Environment variables only, never committed to git |
| 🛡️ Role enforcement | Backend validates roles on every protected endpoint |
| 🔑 JWT validation | Supabase JWT verified on all authenticated routes |
| 🗃️ Data isolation | Supabase RLS ensures users only access their own data |
| 🚫 No sensitive frontend exposure | All protected logic runs server-side |

---

## 🌍 Impact

Forssa is built around a simple belief:

> **Education should not be a privilege.**

- 📖 Equal access to quality education
- 🤝 Empowering underprivileged students
- 🚀 Connecting learning with real-world career opportunities

---

## 📸 Screenshots

> 📁 [View all screenshots on Google Drive](https://drive.google.com/drive/folders/1w3Pcc5fuSLJEhayRsv46X6_6XFh0JPzW?usp=sharing)

---

## 🔗 Related Repositories

| Repo | Link |
|------|------|
| 🎨 Frontend | [Forssa-Frontend](https://github.com/SherifAhmedd/Forssa-Frontend) |
| ⚙️ Backend | [Forssa-Backend](https://github.com/RwanAshrafff/Forssa-Backend) |
| 🌐 Live App | [forssa-frontend.vercel.app](https://forssa-frontend.vercel.app/) |

---

<div align="center">

Made with ❤️ by the Forssa Team

</div>