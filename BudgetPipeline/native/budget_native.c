#include <math.h>

#ifdef _WIN32
#define BUDGET_API __declspec(dllexport)
#define BUDGET_CALL __cdecl
#else
#define BUDGET_API
#define BUDGET_CALL
#endif

/* Returns 0 on success, 1 for a non-positive limit, 2 for invalid spending,
   and 3 when the output pointer is null. Percentage may exceed 100 when over budget. */
BUDGET_API int BUDGET_CALL calculate_utilization_percent(
    double spent,
    double limit,
    double *percentage)
{
    if (percentage == 0) {
        return 3;
    }

    *percentage = 0.0;

    if (!isfinite(spent) || spent < 0.0) {
        return 2;
    }

    if (!isfinite(limit) || limit <= 0.0) {
        return 1;
    }

    *percentage = (spent / limit) * 100.0;
    return 0;
}
