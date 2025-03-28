using SapLichThiITCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapLichThiITCAlgoNew
{

    public class PuddleConditionBuilder
    {
        private Func<Puddle, bool> _condition;

        public Func<Puddle, bool> Finalize()
        {
            return _condition;
        }
        public PuddleConditionBuilder Initialize(Func<Puddle, bool> start)
        {
            _condition = start;
            return this;
        }
        public PuddleConditionBuilder WrapOverChild(Func<Puddle, bool> child)
        {
            _condition = _condition.WrapOverChild(child);
            return this;
        }

        public PuddleConditionBuilder PutInParent(Func<Puddle, bool> parent)
        {
            _condition = _condition.PutInParent(parent);
            return this;
        }
    }
    public static class ExamPuddleConditionExtension
    {

        #region Puddle 
        public static Func<Puddle, bool> GetPuddleHardConstraintCondition(this DatasetXml.Exam exam)
        {
            return (Puddle puddle) =>
            {
                var hardConstraints = exam.Constraints.Where(c => c.IsHard);
                foreach (var constraint in hardConstraints)
                {
                    var constraintType = constraint.Type;
                    var examsInConstraint = constraint.Exams;
                    var constraintSatisfied = true;
                    switch (constraintType)
                    {
                        case DatasetXml.ConstraintType.DifferentRoom:
                            constraintSatisfied = !examsInConstraint.Any(e => e.Assignment.Rooms.Contains(puddle.Room));
                            break;

                        case DatasetXml.ConstraintType.SameRoom:
                            constraintSatisfied = examsInConstraint.Any(e=> e.Assignment.Rooms.Contains(puddle.Room)) || puddle.Exam == null;
                            break;

                        case DatasetXml.ConstraintType.DifferentPeriod:
                            constraintSatisfied = true;
                            break;

                        case DatasetXml.ConstraintType.SamePeriod:
                            constraintSatisfied = true;
                            break;

                        case DatasetXml.ConstraintType.Precedence:
                            constraintSatisfied = true;
                            break;
                    }
                    if (!constraintSatisfied)
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        public static Func<Puddle, bool> GetPuddleSizeCondition(this DatasetXml.Exam exam)
        {
            return (Puddle puddle) =>
            {
                if (puddle.Exam != null)
                    return false;
                if (exam.AltSeating)
                {
                    return puddle.GetRemainingCapacityAlt() > exam.Students.Count;
                }
                else
                {
                    return puddle.GetRemainingCapacity() > exam.Students.Count;
                }
            };
        }

        public static Func<Puddle, bool> GetPuddleRoomAvailableCondition(this DatasetXml.Exam exam)
        {
            return (Puddle puddle) =>
            {
                return (exam.AvailableRooms.FirstOrDefault(ar => ar.Room == puddle.Room) != null);
            };
        }


        public static Comparer<Puddle> GetPuddleSizeComparer(this DatasetXml.Exam exam)
        {
            return Comparer<Puddle>.Create((firstPuddle, secondPuddle) =>
            {
                return exam.AltSeating
                ? firstPuddle.GetRemainingCapacityAlt().CompareTo(secondPuddle.GetRemainingCapacityAlt())
                : firstPuddle.GetRemainingCapacity().CompareTo(secondPuddle.GetRemainingCapacity());
            });
        }

        public static Comparer<Puddle> GetPuddlePenaltyComparer(this DatasetXml.Exam exam)
        {
            return Comparer<Puddle>.Create((firstPuddle, secondPuddle) =>
            {
                return firstPuddle.Penalty.CompareTo(secondPuddle.Penalty);
            });
        }


        public static Comparer<Puddle> GetPuddleSoftContraintComparer(this DatasetXml.Exam exam, DatasetXml.ConstraintType type)
        {
            return Comparer<Puddle>.Create((Puddle firstPuddle, Puddle secondPuddle) =>
            {
                var softConstraint = exam.Constraints.FirstOrDefault(c => !c.IsHard && c.Type == type);
                if (softConstraint == null || type == DatasetXml.ConstraintType.SamePeriod || type == DatasetXml.ConstraintType.DifferentPeriod)
                {
                    return 0;
                }
                var firstPuddlePoint = softConstraint.GetPuddleConstraintTypeSatisfaction(firstPuddle);
                var secondPuddlePoint = softConstraint.GetPuddleConstraintTypeSatisfaction(secondPuddle);
                return firstPuddlePoint.CompareTo(secondPuddlePoint);

            });
        }

        private static int GetPuddleConstraintTypeSatisfaction(this DatasetXml.DistributionConstraint constraint, Puddle puddle)
        {
            switch (constraint.Type)
            {
                case DatasetXml.ConstraintType.DifferentRoom:
                    return !constraint.Exams.Any(e => e.Assignment.Rooms.Contains(puddle.Room)) ? 1 : -1;
                case DatasetXml.ConstraintType.SameRoom:
                    return constraint.Exams.Any(e => e.Assignment.Rooms.Contains(puddle.Room)) ? 1 : -1;
                default:
                    return 0;
            }
        }

        #endregion

        public static Func<Pond, bool> GetPondHardConstraintCondition(this DatasetXml.Exam exam)
        {
            return (Pond pond) =>
            {
                var hardConstraints = exam.Constraints.Where(c => c.IsHard);
                foreach (var constraint in hardConstraints)
                {
                    var constraintType = constraint.Type;
                    var examsInConstraint = constraint.Exams;
                    var constraintSatisfied = true;
                    switch (constraintType)
                    {
                        case DatasetXml.ConstraintType.DifferentPeriod:
                            constraintSatisfied = !examsInConstraint.Any(pond.Exams.Contains);
                            break;

                        case DatasetXml.ConstraintType.SamePeriod:
                            constraintSatisfied = examsInConstraint.Any(pond.Exams.Contains);
                            break;

                        case DatasetXml.ConstraintType.Precedence:
                            constraintSatisfied = !examsInConstraint.Any(pond.Exams.Contains);
                            break;
                    }
                    if (!constraintSatisfied)
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        public static Func<Pond, bool> GetPondStudentAvailabilityCondition(this DatasetXml.Exam exam)
        {
            return (Pond pond) =>
            {
                if (exam.StudentUnavailablePeriods.Contains(pond.Period))
                {
                    return false;
                }
                else return true;
            };
        }

        public static Func<Pond, bool> GetPondInstructorAvailabilityCondition(this DatasetXml.Exam exam)
        {
            return (Pond pond) =>
            {
                if (exam.InstructorUnavailablePeriods.Contains(pond.Period))
                {
                    return false;
                }
                else return true;
            };
        }

        public static Func<Pond, bool> GetPondLinkageCondition(this DatasetXml.Exam exam, Dictionary<DatasetXml.Exam, HashSet<DatasetXml.Exam>> linkages)
        {
            return (Pond pond) =>
            {
                var links = linkages[exam];
                return pond.Exams.Any(e =>
                    links.Contains(e)
                    );
            };
        }

        public static Func<Pond, bool> GetPondDurationCondition(this DatasetXml.Exam exam)
        {
            return (Pond pond) =>
            {
                return pond.Period.Length >= exam.Length;
            };
        }

        public static Func<Pond, bool> GetPondAvailableCondition(this DatasetXml.Exam exam )
        {
            return (Pond pond) =>
            {
                return exam.AvailablePeriods.FirstOrDefault(ap => ap.Period == pond.Period) != null;
            };
        }

        public static Comparer<Pond> GetPondSoftConstraintComparer(this DatasetXml.Exam exam, DatasetXml.ConstraintType type)
        {
            return Comparer<Pond>.Create((firstPond, secondPond) =>
            {
                var softConstraint = exam.Constraints.FirstOrDefault(c => !c.IsHard && c.Type == type);
                if (softConstraint == null || type == DatasetXml.ConstraintType.SameRoom || type == DatasetXml.ConstraintType.DifferentRoom)
                {
                    return 0;
                }
                var firstPondPoint = softConstraint.GetPondConstraintTypeSatisfaction(firstPond);
                var secondPondPoint = softConstraint.GetPondConstraintTypeSatisfaction(secondPond);
                return firstPondPoint.CompareTo(secondPondPoint);
            });
        }

        private static int GetPondConstraintTypeSatisfaction(this DatasetXml.DistributionConstraint constraint, Pond pond)
        {
            switch (constraint.Type)
            {
                case DatasetXml.ConstraintType.DifferentPeriod:
                    return !constraint.Exams.Any(pond.Exams.Contains) ? 1 : -1;
                case DatasetXml.ConstraintType.SamePeriod:
                    return constraint.Exams.Any(pond.Exams.Contains) ? 1 : -1;
                default:
                    return 0;
            }
        }

        public static Comparer<Pond> GetPondDurationComparer(this DatasetXml.Exam exam, bool longerContainerFirst)
        {
            return Comparer<Pond>.Create((firstPond, secondPond) =>
            {
                if (longerContainerFirst)
                    // Normal comparision
                    return firstPond.Period.Length.CompareTo(secondPond.Period.Length);
                // Reverse comparision
                return secondPond.Period.Length.CompareTo(firstPond.Period.Length);
            });
        }

        public static Comparer<Pond> GetPondCapacityComparer(this DatasetXml.Exam exam, bool largerContainerFirst)
        {
            return Comparer<Pond>.Create((firstPond, secondPond) =>
            {
                if (largerContainerFirst)
                    // Normal comparision
                    return firstPond.GetRemainingCapacity().CompareTo(secondPond.GetRemainingCapacity());
                // Reverse comparision
                return secondPond.GetRemainingCapacity().CompareTo(firstPond.GetRemainingCapacity());
            });
        }
    }

}
