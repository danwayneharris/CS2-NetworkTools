# Required implementation documentation for all six plans

Added a shared requirement to plans/README.md: every implementation stage must
produce a durable feature/review document covering usage, architecture, actual
testing, limitations, and Dan's review. Every draft PR must link it. Session notes
and planning documents are not substitutes for the delivered-feature document.

Renamed the shared review heading to Dan's review to match the requested wording.
The six detailed plans are currently in the conversation, not individual files.
This change does not implement or start those plans.

Validation: reviewed the documentation diff and ran git diff --check. No runtime
code changed, so no build or game testing was needed.