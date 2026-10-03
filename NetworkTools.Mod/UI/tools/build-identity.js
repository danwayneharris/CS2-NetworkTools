const fs = require("fs");

exports.loadBuildIdentity = function () {
    const identityPath = process.env.NT_BUILD_IDENTITY;
    if (!identityPath) {
        throw new Error("Build the UI through NetworkTools.csproj so NT_BUILD_IDENTITY matches the assembly. Use -p:NTDeploy=false for a non-deploying package.");
    }
    const identity = JSON.parse(fs.readFileSync(identityPath, "utf8").replace(/^\uFEFF/, ""));
    if (identity.schemaVersion !== 1 || !identity.informationalVersion || !identity.releaseVersion) {
        throw new Error("Invalid NetworkTools build identity: " + identityPath);
    }
    return identity;
};
