# Deployment Checklist - Activity Attendance Code Feature

## ✅ Local Testing (Completed)

- [x] Migration created: `20251116085820_AddAttendanceCodeToActivity`
- [x] Migration applied to local database successfully
- [x] Migration rollback tested successfully
- [x] Migration reapplied successfully
- [x] Solution builds without errors
- [x] All unit tests passing (13/13 tests)

## Migration Details

### Migration File
- **File**: `DataAccess/Migrations/20251116085820_AddAttendanceCodeToActivity.cs`
- **Changes**:
  - Adds `AttendanceCode` column to `Activities` table (nvarchar(6), nullable)
  - Creates index `IX_Activities_AttendanceCode` on the `AttendanceCode` column
  - Includes proper rollback logic in `Down()` method

### Database Schema Changes
```sql
-- Up Migration
ALTER TABLE Activities ADD AttendanceCode NVARCHAR(6) NULL;
CREATE INDEX IX_Activities_AttendanceCode ON Activities(AttendanceCode);

-- Down Migration (Rollback)
DROP INDEX IX_Activities_AttendanceCode ON Activities;
ALTER TABLE Activities DROP COLUMN AttendanceCode;
```

## 📋 Staging Deployment Steps

### Pre-Deployment
1. [ ] Backup staging database
2. [ ] Review migration script one final time
3. [ ] Ensure staging database connection string is configured
4. [ ] Notify team about deployment window

### Deployment
1. [ ] Deploy code to staging environment
   ```bash
   # Build the solution
   dotnet build EduXtend.sln --configuration Release
   
   # Publish WebAPI
   dotnet publish WebAPI/WebAPI.csproj --configuration Release --output ./publish/webapi
   
   # Publish WebFE
   dotnet publish WebFE/WebFE.csproj --configuration Release --output ./publish/webfe
   ```

2. [ ] Run migration on staging database
   ```bash
   dotnet ef database update --project DataAccess --startup-project WebAPI --configuration Release
   ```

3. [ ] Verify migration applied successfully
   ```bash
   dotnet ef migrations list --project DataAccess --startup-project WebAPI
   ```

### Post-Deployment Testing
1. [ ] Test Activity creation - verify AttendanceCode is generated
2. [ ] Test Student check-in with valid code
3. [ ] Test Student check-in with invalid code
4. [ ] Test time window validation (before/after activity time)
5. [ ] Test Admin/Manager score adjustment
6. [ ] Test AttendanceCode visibility (Admin/Manager can see, Student cannot)
7. [ ] Verify MovementRecord updates correctly
8. [ ] Test API endpoints:
   - `POST /api/activities` - Create activity with code
   - `GET /api/activities/{id}` - View activity with code filtering
   - `POST /api/activities/{id}/check-in` - Student check-in
   - `PATCH /api/activities/{activityId}/attendance/{userId}` - Update score

### Rollback Plan (If Needed)
```bash
# Rollback to previous migration
dotnet ef database update 20251114110447_AddOtherScoreToClubMovementRecord --project DataAccess --startup-project WebAPI

# Restore previous code version
```

## 🚀 Production Deployment Steps

### Pre-Deployment
1. [ ] Backup production database (CRITICAL)
2. [ ] Schedule maintenance window
3. [ ] Notify users about potential downtime
4. [ ] Verify staging tests completed successfully
5. [ ] Prepare rollback plan

### Deployment
1. [ ] Deploy code to production environment
2. [ ] Run migration on production database
   ```bash
   dotnet ef database update --project DataAccess --startup-project WebAPI --configuration Release
   ```
3. [ ] Verify migration applied successfully

### Post-Deployment Monitoring
1. [ ] Monitor application logs for errors
2. [ ] Check database performance (index usage)
3. [ ] Verify API response times
4. [ ] Monitor user feedback
5. [ ] Test critical user flows

### Health Checks
- [ ] Application starts successfully
- [ ] Database connections working
- [ ] All API endpoints responding
- [ ] No error spikes in logs
- [ ] Performance metrics within acceptable range

## 🔧 Troubleshooting

### If Migration Fails
1. Check database connection string
2. Verify database user has sufficient permissions
3. Check for conflicting migrations
4. Review error logs
5. Consider manual SQL execution if needed

### If Application Fails
1. Check application logs
2. Verify all dependencies deployed
3. Check configuration files (appsettings.json)
4. Verify database connection
5. Consider rollback if critical

## 📊 Success Criteria

- [ ] Migration applied without errors
- [ ] All existing functionality works
- [ ] New attendance code feature works as expected
- [ ] No performance degradation
- [ ] No error spikes in logs
- [ ] User feedback is positive

## 📝 Notes

- The `AttendanceCode` column is nullable to support existing activities
- Index on `AttendanceCode` improves lookup performance
- Migration is reversible via the `Down()` method
- All 13 unit tests passing on local environment
- Feature has been fully implemented and tested locally

## 🔗 Related Documentation

- Requirements: `.kiro/specs/activity-attendance-code/requirements.md`
- Design: `.kiro/specs/activity-attendance-code/design.md`
- Tasks: `.kiro/specs/activity-attendance-code/tasks.md`
