param (
    [string]$branch
)

if (-not $branch) {
    Write-Host "Usage: .\switch-branch.ps1 <branch>"
    exit 1
}

if ($branch -eq "main") {
    Write-Host "Switching to 'main' branch and disabling sparse checkout..."
    git checkout main
    git sparse-checkout disable
    exit 0
}

Write-Host "Switching to branch '$branch' and configuring sparse checkout..."
git checkout $branch

# Set sparse-checkout to include root-level files (".") and the branch-specific folder ("$branch/")
git sparse-checkout set "$branch/"
git sparse-checkout add sb.ps1

git sparse-checkout reapply

Write-Host "Sparse checkout configured: only root-level files and the '$branch/' folder are checked out."
