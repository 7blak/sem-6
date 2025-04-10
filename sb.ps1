param (
    [string]$branch,
	[switch]$nopull
)

$inGitRepo = git rev-parse --is-inside-work-tree 2>$null

if ($inGitRepo -ne "true") {
	Write-Host "Error: This is not a Git repository."
	exit 1
}

if (-not $branch) {
    Write-Host "Usage: .\switch-branch.ps1 <branch>"
    exit 1
}

$localBranch = git branch --list $branch | Out-String
$remoteBranch = git branch -r --list "origin/$branch" | Out-String

if (($localBranch.Trim() -eq "") -and ($remoteBranch.Trim() -eq "")) {
    Write-Host "Error: Branch '$branch' does not exist locally or remotely."
    exit 1
}

if ($branch -eq "main") {
    Write-Host "Switching to 'main' branch and disabling sparse checkout..."
    git checkout main
    git sparse-checkout disable
    exit 0
}

Write-Host "Switching to branch '$branch' and configuring sparse checkout..."
git submodule --quiet deinit -f . >$null 2>&1

git checkout $branch --

git sparse-checkout set "$branch/"

git sparse-checkout reapply

if (-not $nopull) {
	Write-Host "Populating submodules..."
	$submodules = git config --file .gitmodules --get-regexp path | ForEach-Object {
		$_.Split(" ")[1]
	}
	
	foreach ($sub in $submodules) {
		if (Test-Path $sub) {
			Write-Host "Updating submodule: $sub"
			git submodule update --init --recursive -- "$sub"
		} else {
			Write-Host "Skipping submodule not in sparse-checkout: $sub"
		}
	}
}

Write-Host "Sparse checkout configured: only root-level files and the '$branch/' folder are checked out."
