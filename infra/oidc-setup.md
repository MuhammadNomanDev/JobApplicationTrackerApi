# P1/M2: OIDC setup guide (Noman does this in Azure portal)

GitHub Actions logs in to Azure without any stored secret. Instead, Azure
trusts GitHub's OIDC token for this specific repo + branch. Steps:

## 1. Create the Microsoft Entra app

1. Azure portal → **Microsoft Entra ID** → **App registrations** → **New registration**.
2. Name: `jobtracker-github-actions`. Leave everything else default. **Register**.
3. On the app's **Overview** page, copy:
   - **Application (client) ID** → this is `AZURE_CLIENT_ID`
   - **Directory (tenant) ID** → this is `AZURE_TENANT_ID`
4. Go to **Subscriptions** → your subscription → copy the **Subscription ID** → `AZURE_SUBSCRIPTION_ID`.

## 2. Give it Contributor on the resource group

1. Go to **Resource groups** → `rg-jobtracker` → **Access control (IAM)**.
2. **Add** → **Add role assignment**.
3. Role: **Contributor**. Assign access to: **User, group, or service principal**.
4. Select: `jobtracker-github-actions`. **Review + assign**.

(Contributor on the resource group is enough — it can restart the Web App
and read outputs, but cannot touch anything outside `rg-jobtracker`.)

## 3. Add the federated credential (the OIDC trust)

1. Back in the app registration → **Certificates & secrets** → **Federated credentials** → **Add credential**.
2. Federated credential scenario: **GitHub Actions deploying Azure resources**.
3. Fill in:
   - Organization: `MuhammadNomanDev`
   - Repository: `JobApplicationTrackerApi`
   - Entity type: **Branch**
   - GitHub branch name: `main`
   - Name: `jobtracker-main`
4. **Add**.

## 4. Add the three secrets to GitHub

Repo → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**:

| Name | Value |
|---|---|
| `AZURE_CLIENT_ID` | Application (client) ID from step 1 |
| `AZURE_TENANT_ID` | Directory (tenant) ID from step 1 |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID from step 1 |

No `AZURE_CREDENTIALS` — that was the old long-lived-secret way. OIDC mints
a short-lived token per workflow run instead.

## 5. Replace the deploy job in the workflow

`.github/workflows/ci-cd.yml` → replace the entire `deploy:` job with the
contents of `infra/deploy-job.yml` (on the `p1/m2-azure` branch). The App
cannot edit workflows, so this is a web-UI paste.

## 6. Configure App Service app settings

In the Azure portal → `jobtracker-noman-dev-api-akbudaaudcheefgd` → **Configuration** → **Application settings**,
add (from the Bicep deployment outputs + your own values):

| Name | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL connection string (Bicep output `sqlConnectionString`) |
| `AzureBlobStorage__ConnectionString` | Storage connection string (Bicep output `storageConnectionString`) |
| `AzureBlobStorage__ContainerName` | `documents` |
| `Jwt__Key` | 32+ character random string (generate: `openssl rand -base64 32`) |
| `Jwt__Issuer` | `JobApplicationTracker` |
| `Jwt__Audience` | `JobApplicationTracker` |

(`__` is how App Service maps to .NET's `:` config hierarchy.)

Then push to `main` — the workflow deploys the container and restarts the app.
Check `https://jobtracker-noman-dev-api-akbudaaudcheefgd.westus3-01.azurewebsites.net/health/ready`.
