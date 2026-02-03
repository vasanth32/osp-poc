# PowerShell Quick Commands for Application Insights

## ⚠️ Important: PowerShell Syntax

In PowerShell, use **backticks** (`` ` ``) for line continuation, NOT backslashes (`\`).

---

## 🚀 Quick Setup Commands (PowerShell)

### **1. Login to Azure**
```powershell
az login
```

### **2. Create Resource Group**
```powershell
az group create --name rg-fee-management --location eastus
```

### **3. Create Application Insights**
```powershell
az monitor app-insights component create --app fee-management-service-insights --location eastus --resource-group rg-fee-management --application-type web
```

### **4. Get Connection String**
```powershell
az monitor app-insights component show --app fee-management-service-insights --resource-group rg-fee-management --query connectionString --output tsv
```

---

## 📝 All-in-One Script (PowerShell)

Copy and paste this entire block:

```powershell
# Set variables
$resourceGroup = "rg-fee-management"
$appInsightsName = "fee-management-service-insights"
$location = "eastus"

# Login (if not already logged in)
az login

# Create resource group
az group create --name $resourceGroup --location $location

# Create Application Insights
az monitor app-insights component create --app $appInsightsName --location $location --resource-group $resourceGroup --application-type web

# Get connection string
az monitor app-insights component show --app $appInsightsName --resource-group $resourceGroup --query connectionString --output tsv
```

---

## 🔄 Using Backticks for Multi-Line (Optional)

If you prefer multi-line commands, use backticks (`` ` ``):

```powershell
# Create Application Insights (multi-line)
az monitor app-insights component create `
  --app fee-management-service-insights `
  --location eastus `
  --resource-group rg-fee-management `
  --application-type web
```

**Note:** The backtick must be the **last character** on the line (no spaces after it).

---

## ✅ Correct vs Incorrect

### ❌ **WRONG** (Backslash - Unix/Linux syntax)
```powershell
az group create \ --name rg-fee-management \ --location eastus
```

### ✅ **CORRECT** (Single line - Recommended for PowerShell)
```powershell
az group create --name rg-fee-management --location eastus
```

### ✅ **CORRECT** (Backticks for line continuation)
```powershell
az group create `
  --name rg-fee-management `
  --location eastus
```

---

## 🎯 Recommended Approach for PowerShell

**Use single-line commands** - they're cleaner and less error-prone in PowerShell:

```powershell
az group create --name rg-fee-management --location eastus
az monitor app-insights component create --app fee-management-service-insights --location eastus --resource-group rg-fee-management --application-type web
az monitor app-insights component show --app fee-management-service-insights --resource-group rg-fee-management --query connectionString --output tsv
```

---

## 🔍 Verify Your Setup

```powershell
# Check if resource group exists
az group show --name rg-fee-management

# Check if Application Insights exists
az monitor app-insights component show --app fee-management-service-insights --resource-group rg-fee-management --output table

# List all Application Insights in your subscription
az monitor app-insights component list --output table
```

---

## ⚠️ Common Prompts

### **"The command requires the extension application-insights. Do you want to install it now? (Y/n):"**

**Answer:** Type `Y` and press Enter. This is normal - Azure CLI needs to install the Application Insights extension to run the command.

**To avoid this prompt in the future:**
```powershell
az config set extension.use_dynamic_install=yes_without_prompt
```

---

**That's it! Use single-line commands in PowerShell for best results! 🎉**

