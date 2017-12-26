using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    struct FloatN
    {
        public FloatN(float a) : this(new[] { a })
        {
        }
        public FloatN(Vector2 a) : this(new[] { a.x, a.y })
        {
        }
        public FloatN(Vector3 a) : this(new[] { a.x, a.y, a.z })
        {
        }
        public FloatN(Vector4 a) : this(new[] { a.x, a.y, a.z, a.w })
        {
        }
        public FloatN(float[] currentValues = null)
        {
            m_Components = currentValues;
        }

        public float this[int i]
        {
            get
            {
                if (realSize == 1)
                {
                    return m_Components[0];
                }
                return realSize > i ? m_Components[i] : 0.0f;
            }
            set
            {
                if (realSize > i)
                {
                    m_Components[i] = value;
                }
            }
        }

        public float x { get { return this[0]; } set { this[0] = value; } }
        public float y { get { return this[1]; } set { this[1] = value; } }
        public float z { get { return this[2]; } set { this[2] = value; } }
        public float w { get { return this[3]; } set { this[3] = value; } }
        public int realSize { get { return m_Components == null ? 0 : m_Components.Length; } }

        public static implicit operator FloatN(float value)
        {
            return new FloatN(new[] { value });
        }

        public static implicit operator FloatN(Vector2 value)
        {
            return new FloatN(new[] { value.x, value.y });
        }

        public static implicit operator FloatN(Vector3 value)
        {
            return new FloatN(new[] { value.x, value.y, value.z });
        }

        public static implicit operator FloatN(Vector4 value)
        {
            return new FloatN(new[] { value.x, value.y, value.z, value.w });
        }

        public static implicit operator float(FloatN value)
        {
            return value.x;
        }

        public static implicit operator Vector2(FloatN value)
        {
            return new Vector2(value.x, value.y);
        }

        public static implicit operator Vector3(FloatN value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        public static implicit operator Vector4(FloatN value)
        {
            return new Vector4(value.x, value.y, value.z, value.w);
        }

        public VFXValue ToVFXValue(VFXValue.Mode mode)
        {
            switch (realSize)
            {
                case 1: return new VFXValue<float>(this, mode);
                case 2: return new VFXValue<Vector2>(this, mode);
                case 3: return new VFXValue<Vector3>(this, mode);
                case 4: return new VFXValue<Vector4>(this, mode);
            }
            return null;
        }

        [SerializeField]
        private float[] m_Components;
    }

    abstract class VFXOperatorFloatUnified : VFXOperator
    {
        protected float m_FallbackValue = 0.0f;

        protected VFXOperatorFloatUnified()
        {
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                const string outputName = "o";
                Type slotType = null;
                switch (GetFloatMaxNbComponents(inputSlots))
                {
                    case 1: slotType = typeof(float); break;
                    case 2: slotType = typeof(Vector2); break;
                    case 3: slotType = typeof(Vector3); break;
                    case 4: slotType = typeof(Vector4); break;
                    default: break;
                }

                if (slotType != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, outputName));
            }
        }

        public override void OnEnable()
        {
            var propertyType = GetType().GetRecursiveNestedType(GetInputPropertiesTypeName());
            if (propertyType != null)
            {
                var fields = propertyType.GetFields().Where(o => o.IsStatic && o.Name == "FallbackValue");
                var field = fields.FirstOrDefault(o => o.FieldType == typeof(float));
                if (field != null)
                {
                    m_FallbackValue = (float)field.GetValue(null);
                }
            }

            base.OnEnable();
        }

        //Convert automatically input expression with diverging floatN size to floatMax
        override protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            var inputExpression = GetRawInputExpressions();
            return VFXOperatorUtility.UnifyFloatLevel(inputExpression, m_FallbackValue);
        }
    }

    abstract class VFXOperatorUnaryFloatOperation : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            [Tooltip("The operand.")]
            public FloatN x = new FloatN(0.0f);
        }
    }

    abstract class VFXOperatorBinaryFloatCascadableOperation : VFXOperatorFloatUnified
    {
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                int nbNeededSlots = 2;
                var currentSlots = inputSlots.ToList();
                for (int i = 0; i < currentSlots.Count; ++i)
                    if (currentSlots[i].HasLink())
                        nbNeededSlots = Math.Max(nbNeededSlots, i + 2);
                // +2 to reserve an unlinked slot

                for (int i = 0; i < nbNeededSlots; ++i)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(FloatN), ((char)((int)'a' + i)).ToString()), new FloatN(m_FallbackValue));
            }
        }

        public override void UpdateOutputExpressions()
        {
            var inputExpression = GetInputExpressions();
            //Process aggregate two by two element until result
            var outputExpression = new Stack<VFXExpression>(inputExpression.Reverse());
            while (outputExpression.Count > 1)
            {
                var a = outputExpression.Pop();
                var b = outputExpression.Pop();
                var compose = BuildExpression(new[] { a, b })[0];
                outputExpression.Push(compose);
            }
            SetOutputExpressions(outputExpression.ToArray());
        }
    }

    abstract class VFXOperatorBinaryFloatOperationOne : VFXOperatorBinaryFloatCascadableOperation
    {
        public class InputProperties
        {
            static public float FallbackValue = 1.0f;
            [Tooltip("The first operand.")]
            public FloatN a = FallbackValue;
            [Tooltip("The second operand.")]
            public FloatN b = FallbackValue;
        }
    }

    abstract class VFXOperatorBinaryFloatOperationZero : VFXOperatorBinaryFloatCascadableOperation
    {
        public class InputProperties
        {
            static public float FallbackValue = 0.0f;
            [Tooltip("The first operand.")]
            public FloatN a = FallbackValue;
            [Tooltip("The second operand.")]
            public FloatN b = FallbackValue;
        }
    }
}
