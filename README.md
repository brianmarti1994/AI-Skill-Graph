# 🔍 AI Skill Graph
<p align="center">
<a href="https://youtu.be/hrR9PUL-Ge0" target="_blank">
  <img
    width="400"
    height="280"
    alt="Watch demo video"
    src="https://github.com/user-attachments/assets/644f15e3-73e2-4cfd-a18e-0dd86b844792"
  />
</a>
</p>

> **AI-powered CV analysis & skill matching platform**  
> Upload a resume, define a target role, and instantly get:
> - Skill extraction
> - Experience estimation
> - Job match percentage
> - GitHub project analysis
> - Visual skill graph

Built with **.NET 8**, **Blazor WebAssembly**, **SQLite**, **Ollama AI**, and **Chart.js**.

---

## ✨ Key Features

### 📄 Intelligent CV Parsing
- Upload **PDF / DOCX / TXT**
- AI extracts:
  - Candidate profile
  - Skills with years of experience
  - Employment history
- Handles **implicit experience** using AI estimation

### 🎯 Job Match Scoring
- Provide a **target role prompt** (e.g. `.NET Developer`)
- Optional **must-have skills CSV**
- Calculates:
  - Match percentage
  - Traffic-light result (Green / Yellow / Red)

### 📊 Skill Visualization
- Interactive **skill graph**
- Shows **Top 15 skills by experience**
- Easy to interpret at a glance

### 🧑‍💻 GitHub Repository Analysis
- Automatically finds GitHub profile from CV
- Scans repositories
- Displays:
  - Repo names
  - Technology usage
  - Language distribution

### 🧠 Powered by Local AI (Ollama)
- Uses **Ollama** for privacy-friendly AI
- No external cloud dependency
- Fully customizable prompts & models

---

## 🖼️ UI Overview

- **Input Section** – CV upload & role configuration  
- **Result Section** – Candidate summary & KPIs  
- **Git Repo Panel** – Project intelligence  
- **Skill Graph** – Visual tech stack breakdown  

Designed for **recruiters, hiring managers, and engineers**.

---

## 🏗️ Architecture
<img width="544" height="172" alt="image" src="https://github.com/user-attachments/assets/579b1ef3-35a3-4f7e-b668-9bf1cccfd443" />


**Architecture Style:**  
✔ Clean Architecture  
✔ SOLID principles  
✔ Dependency Injection  

---

## 🧩 Tech Stack

### Backend
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Ollama AI (local LLM)

### Frontend
- Blazor WebAssembly
- Chart.js
- CSS Grid / Flexbox

### AI & Integrations
- Ollama (LLM-based CV parsing)
- GitHub REST API

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Ollama installed & running
- SQLite (auto-created)



