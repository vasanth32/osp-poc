# Create Application Insights Using Azure CLI

This guide shows you how to create an Application Insights resource using Azure CLI.

---

## 📋 Prerequisites

1. **Install Azure CLI**
   - Windows: Download from [Azure CLI](https://aka.ms/installazurecliwindows)
   - Mac: `brew install azure-cli`
   - Linux: Follow [Azure CLI Installation Guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)

2. **Verify Installation**
   ```bash
   az --version
   ```

---

## 🚀 Step-by-Step: Create Application Insights with Azure CLI

### **Step 1: Login to Azure**

```bash
az login
```

This will open a browser window for authentication. After logging in, you'll see your subscriptions.

### **Step 2: Set Your Subscription (Optional)**

If you have multiple subscriptions, set the active one:

```bash
# List all subscriptions
az account list --output table

# Set active subscription
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

### **Step 3: Create Resource Group (If Needed)**

**For Bash/Linux/Mac:**
```bash
# Create a new resource group
az group create \
  --name rg-fee-management \
  --location eastus

# Or use an existing resource group
# List existing resource groups
az group list --output table
```

**For Windows PowerShell:**
```powershell
# Create a new resource group (single line)
az group create --name rg-fee-management --location eastus

# Or with line continuation using backticks
az group create `
  --name rg-fee-management `
  --location eastus

# List existing resource groups
az group list --output table
```

**Common Locations:**
- `eastus` - East US
- `westus` - West US
- `westeurope` - West Europe
- `southeastasia` - Southeast Asia
- `centralindia` - Central India

### **Step 4: Create Application Insights Resource**

**For Bash/Linux/Mac:**
```bash
az monitor app-insights component create \
  --app fee-management-service-insights \
  --location eastus \
  --resource-group rg-fee-management \
  --application-type web
```

**For Windows PowerShell:**
```powershell
# Single line (recommended for PowerShell)
az monitor app-insights component create --app fee-management-service-insights --location eastus --resource-group rg-fee-management --application-type web

# Or with line continuation using backticks
az monitor app-insights component create `
  --app fee-management-service-insights `
  --location eastus `
  --resource-group rg-fee-management `
  --application-type web
```

**Parameters Explained:**
- `--app`: Name of your Application Insights resource (must be globally unique)
- `--location`: Azure region (use same as resource group)
- `--resource-group`: Your resource group name
- `--application-type`: Type of application (`web` for ASP.NET Core)

**Alternative: More Detailed Creation**

**For Bash/Linux/Mac:**
```bash
az monitor app-insights component create \
  --app fee-management-service-insights \
  --location eastus \
  --resource-group rg-fee-management \
  --application-type web \
  --kind web \
  --retention-time 90
```

**For Windows PowerShell:**
```powershell
# Single line
az monitor app-insights component create --app fee-management-service-insights --location eastus --resource-group rg-fee-management --application-type web --kind web --retention-time 90

# Or with backticks
az monitor app-insights component create `
  --app fee-management-service-insights `
  --location eastus `
  --resource-group rg-fee-management `
  --application-type web `
  --kind web `
  --retention-time 90
```

**Additional Parameters:**
- `--kind`: Type of component (`web` for web applications)
- `--retention-time`: Data retention in days (30, 60, 90, 120, 180, 270, 365, 550, 730)

### **Step 5: Get Connection String**

**For Bash/Linux/Mac:**
```bash
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --query connectionString \
  --output tsv
```

**For Windows PowerShell:**
```powershell
# Single line (recommended)
az monitor app-insights component show --app fee-management-service-insights --resource-group rg-fee-management --query connectionString --output tsv

# Or with backticks
az monitor app-insights component show `
  --app fee-management-service-insights `
  --resource-group rg-fee-management `
  --query connectionString `
  --output tsv
```

**Copy the connection string** - you'll need it for your `appsettings.json`!

### **Step 6: Verify Creation**

**For Bash/Linux/Mac:**
```bash
# List all Application Insights resources
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --output table
```

**For Windows PowerShell:**
```powershell
# Single line
az monitor app-insights component show --app fee-management-service-insights --resource-group rg-fee-management --output table

# Or with backticks
az monitor app-insights component show `
  --app fee-management-service-insights `
  --resource-group rg-fee-management `
  --output table
```

---

## 🎯 Complete Script (One-Liner Approach)

If you want to do everything in one go:

```bash
# Set variables
RESOURCE_GROUP="rg-fee-management"
APP_INSIGHTS_NAME="fee-management-service-insights"
LOCATION="eastus"

# Create resource group (if it doesn't exist)
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get connection string
az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv
```

**For Windows PowerShell:**

```powershell
# Set variables
$resourceGroup = "rg-fee-management"
$appInsightsName = "fee-management-service-insights"
$location = "eastus"

# Create resource group (if it doesn't exist)
az group create --name $resourceGroup --location $location

# Create Application Insights
az monitor app-insights component create `
  --app $appInsightsName `
  --location $location `
  --resource-group $resourceGroup `
  --application-type web

# Get connection string
az monitor app-insights component show `
  --app $appInsightsName `
  --resource-group $resourceGroup `
  --query connectionString `
  --output tsv
```

---

## 📝 Add Connection String to Your App

### **Option 1: Update appsettings.json**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/"
  }
}
```

### **Option 2: Use Environment Variable**

**Windows PowerShell:**
```powershell
$env:APPLICATIONINSIGHTS_CONNECTION_STRING="YOUR_CONNECTION_STRING_HERE"
```

**Linux/Mac:**
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="YOUR_CONNECTION_STRING_HERE"
```

### **Option 3: User Secrets (Development)**

```bash
dotnet user-secrets set "ApplicationInsights:ConnectionString" "YOUR_CONNECTION_STRING_HERE"
```

---

## 🔍 Useful Azure CLI Commands

### **List All Application Insights Resources**

```bash
az monitor app-insights component list --output table
```

### **Get All Details of Your Resource**

```bash
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --output json
```

### **Update Application Insights Settings**

```bash
# Update retention time
az monitor app-insights component update \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --retention-time 90
```

### **Delete Application Insights Resource**

```bash
az monitor app-insights component delete \
  --app fee-management-service-insights \
  --resource-group rg-fee-management
```

### **Get Instrumentation Key (Alternative to Connection String)**

```bash
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --query instrumentationKey \
  --output tsv
```

---

## 🐛 Troubleshooting

### **Prompt: "The command requires the extension application-insights. Do you want to install it now? (Y/n):"**

**Solution:** This is normal! Type `Y` and press Enter to install the extension. Azure CLI will automatically install it and then proceed with creating Application Insights.

**To enable automatic extension installation (optional):**
```powershell
# Allow automatic installation of extensions
az config set extension.use_dynamic_install=yes_without_prompt

# Or allow preview extensions (if needed)
az config set extension.dynamic_install_allow_preview=true
```

### **Error: "The subscription is not registered to use namespace 'Microsoft.Insights'"**

**Solution:** Register the resource provider:

```bash
az provider register --namespace Microsoft.Insights
az provider register --namespace Microsoft.OperationalInsights
```

Wait a few minutes, then try again.

### **Error: "Name already exists"**

**Solution:** Application Insights names must be globally unique. Try a different name:

```bash
az monitor app-insights component create \
  --app fee-management-service-insights-$(date +%s) \
  --location eastus \
  --resource-group rg-fee-management \
  --application-type web
```

### **Error: "Resource group not found"**

**Solution:** Create the resource group first:

```bash
az group create --name rg-fee-management --location eastus
```

### **Check if Application Insights Already Exists**

```bash
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --query name \
  --output tsv
```

If it returns the name, it exists. If it returns nothing, it doesn't exist.

---

## ✅ Verification Checklist

After creating Application Insights:

- [ ] Resource created successfully
- [ ] Connection string retrieved
- [ ] Connection string added to `appsettings.json` or environment variable
- [ ] Application runs without errors
- [ ] Telemetry appears in Azure Portal (wait 1-2 minutes)

---

## 🎓 Learning Exercise

Try these commands to explore your Application Insights resource:

```bash
# 1. Get all properties
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --output json | jq .

# 2. List all Application Insights in your subscription
az monitor app-insights component list --output table

# 3. Get resource ID (useful for other commands)
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --query id \
  --output tsv
```

---

## 📚 Additional Resources

- [Azure CLI Documentation](https://learn.microsoft.com/en-us/cli/azure/)
- [Application Insights CLI Commands](https://learn.microsoft.com/en-us/cli/azure/monitor/app-insights)
- [Azure CLI Installation Guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)

---

## 🚀 Quick Reference Card

**For Bash/Linux/Mac:**
```bash
# Login
az login

# Create Resource Group
az group create --name rg-fee-management --location eastus

# Create Application Insights
az monitor app-insights component create \
  --app fee-management-service-insights \
  --location eastus \
  --resource-group rg-fee-management \
  --application-type web

# Get Connection String
az monitor app-insights component show \
  --app fee-management-service-insights \
  --resource-group rg-fee-management \
  --query connectionString \
  --output tsv
```

**For Windows PowerShell:**
```powershell
# Login
az login

# Create Resource Group
az group create --name rg-fee-management --location eastus

# Create Application Insights
az monitor app-insights component create --app fee-management-service-insights --location eastus --resource-group rg-fee-management --application-type web

# Get Connection String
az monitor app-insights component show --app fee-management-service-insights --resource-group rg-fee-management --query connectionString --output tsv
```

**That's it! You now have Application Insights set up via Azure CLI! 🎉**

