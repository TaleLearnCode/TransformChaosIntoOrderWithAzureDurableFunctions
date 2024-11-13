# Define the paths
$terraformFolder = ".terraform"
$terraformLockFile = ".terraform.lock.hcl"
$tfplanFiles = "*.tfplan"
$tfstateFiles = "*.tfstate*"


# Delete the .terraform folder and its contents
if (Test-Path $terraformFolder) {
    Remove-Item -Recurse -Force $terraformFolder
}

# Delete the .terraform.lock.hcl file
if (Test-Path $terraformLockFile) {
    Remove-Item -Force $terraformLockFile
}

# Delete any .tfplan files
$tfplanFilesList = Get-ChildItem -Path . -Filter $tfplanFiles
if ($tfplanFilesList) {
    $tfplanFilesList | Remove-Item -Force
}

# Delete any .tfstate files
$tfplanFilesList = Get-ChildItem -Path . -Filter $tfstateFiles
if ($tfplanFilesList) {
    $tfplanFilesList | Remove-Item -Force
}

# Prompt the user for the environment
$environment = Read-Host -Prompt 'Enter the environment'

# Retrieve the current Azure subscription ID
$subscriptionId = (az account show --query id --output tsv)


# Initialize Terraform
terraform init
if ($LASTEXITCODE -ne 0) {
    Write-Error "Terraform init failed. Stopping execution."
    exit $LASTEXITCODE
}

# Call terraform apply with the subscription ID and environment
terraform apply --var-file=$environment.tfvars -var="subscription_id=$subscriptionId"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Terraform apply failed. Stopping execution."
    exit $LASTEXITCODE
}